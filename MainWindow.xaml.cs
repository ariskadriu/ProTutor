using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Linq;
using System.Collections.Generic;
using ProgrammingTutor.Core;
using ProgrammingTutor.Models;
using ProgrammingTutor.Services;
using System.Windows.Media;

namespace ProgrammingTutor
{
    public partial class MainWindow : Window
    {
        private readonly LessonManager _lessonManager;
        private readonly JsonStorageService _storageService;
        private readonly CodeRunner _codeRunner;
        private readonly ExerciseValidator _validator;
        private readonly AIHintService _aiHintService;
        private Lesson? _currentLesson;
        private int _currentChallengeIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
            
            _lessonManager = new LessonManager();
            _storageService = new JsonStorageService();
            _codeRunner = new CodeRunner();
            _validator = new ExerciseValidator(_codeRunner, _storageService);
            _aiHintService = new AIHintService();
            
            UserText.Text = App.CurrentUser?.Username ?? "Guest";
            UpdateLocalization();

            // Set default highlighting
            Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

            LoadLessonsToUI();
        }

        private void UpdateLocalization()
        {
            bool isAl = App.CurrentLanguage == "sq";
            SidebarHeader.Text = isAl ? "PROGRESIONI IM" : "MY PROGRESS";
            CheckAnswerButton.Content = isAl ? "DËRGO CHALLENGE" : "SUBMIT CHALLENGE";
            AiHintButton.Content = isAl ? "💡 NDIHMË AI" : "💡 HINT";
            ChallengeHeader.Text = isAl ? "SFIDA" : "CHALLENGE";
            LessonTitleText.Text = isAl ? "Mirësevini!" : "Welcome!";
            LessonDescriptionText.Text = isAl ? "Ju lutem zgjidhni një mësim për të filluar mësimin." : "Please select a lesson to start learning.";
            ExerciseText.Text = isAl ? "Gati për të testuar aftësitë tuaja?" : "Ready to test your skills?";
            
            FileMenu.Header = isAl ? "SKEDARI" : "FILE";
            ExitItem.Header = isAl ? "Dil" : "Exit";
            LearnMenu.Header = isAl ? "MËSO" : "LEARN";
            SettingsMenu.Header = isAl ? "CILËSIMET" : "SETTINGS";
        }

        private void LoadLessonsToUI()
        {
            _lessonManager.LoadLessons();
            LessonTreeView.Items.Clear();

            string trackFilter = App.SelectedTrack;
            foreach (var catId in _lessonManager.GetCategories())
            {
                if (catId != trackFilter) continue;

                string displayCategory = catId == "py" ? "PYTHON" : (catId == "cs" ? "C# (.NET)" : catId.ToUpper());
                var categoryItem = new TreeViewItem 
                { 
                    Header = displayCategory,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontWeight = FontWeights.Bold,
                    IsExpanded = true
                };

                var lessons = _lessonManager.GetLessonsByCategory(catId);
                foreach (var lesson in lessons)
                {
                    if (lesson.Id.EndsWith($"_{App.CurrentLanguage}"))
                    {
                        var lessonItem = new TreeViewItem 
                        { 
                            Header = lesson.Title,
                            Tag = lesson.Id,
                            Foreground = System.Windows.Media.Brushes.LightGray
                        };
                        categoryItem.Items.Add(lessonItem);
                    }
                }
                if (categoryItem.Items.Count > 0)
                    LessonTreeView.Items.Add(categoryItem);
            }

            UpdateProgressStats();
        }

        private async void UpdateProgressStats()
        {
            if (_lessonManager == null) return;
            
            var lessons = _lessonManager.GetLessons().Where(l => l.Id.EndsWith($"_{App.CurrentLanguage}")).ToList();
            int total = lessons.Count;
            
            string username = App.CurrentUser?.Username ?? "Guest";
            string progressFile = $"progress_{username}.json";
            
            // Try to load from local first
            var progress = await _storageService.LoadAsync<Progress>(progressFile) ?? new Progress();

            // Try to sync with cloud if online
            var cloudSync = new CloudSyncService();
            var cloudProgress = await cloudSync.DownloadProgressFromServerAsync(username);
            
            if (cloudProgress != null && cloudProgress.CompletedLessons.Count > progress.CompletedLessons.Count)
            {
                progress = cloudProgress;
                await _storageService.SaveAsync(progressFile, progress);
            }
            
            int completed = progress.CompletedLessons.Count(id => id.EndsWith($"_{App.CurrentLanguage}"));

            if (ProgressStats != null)
            {
                ProgressStats.Text = App.CurrentLanguage == "sq" 
                    ? $"U plotësuan {completed} nga {total} mësime" 
                    : $"{completed} of {total} lessons completed";
            }
        }

        private void LessonTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is string lessonId)
            {
                _currentLesson = _lessonManager.GetLessonById(lessonId);
                if (_currentLesson != null)
                {
                    _currentChallengeIndex = 0;
                    UpdateChallengeUI();

                    LangLabel.Text = _currentLesson.Id.StartsWith("py") ? "PYTHON" : "C#";
                    LangBadge.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(_currentLesson.Id.StartsWith("py") ? "#3776AB" : "#178600")!;

                    string highlightLang = _currentLesson.Id.StartsWith("py") ? "Python" : "C#";
                    Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(highlightLang);
                }
            }
        }

        private void UpdateChallengeUI()
        {
            if (_currentLesson == null || _currentLesson.Challenges == null || _currentLesson.Challenges.Count == 0) return;

            var challenge = _currentLesson.Challenges[_currentChallengeIndex];
            LessonTitleText.Text = $"{_currentLesson.Title}";
            LessonDescriptionText.Text = _currentLesson.Description;
            
            string challengePrefix = App.CurrentLanguage == "sq" ? "SFIDA" : "CHALLENGE";
            ChallengeHeader.Text = $"{challengePrefix} {_currentChallengeIndex + 1}/{_currentLesson.Challenges.Count}: {challenge.Title} ({challenge.Difficulty})";
            ExerciseText.Text = challenge.Exercise;
            
            if (_currentChallengeIndex == 0)
                Editor.Text = _currentLesson.Example;
        }

        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLesson == null)
            {
                OutputTextBox.Clear();
                OutputTextBox.Foreground = System.Windows.Media.Brushes.Orange;
                OutputTextBox.AppendText(App.CurrentLanguage == "sq" 
                    ? "Ju lutem zgjidhni një mësim në të majtë para se të ekzekutoni kodin." 
                    : "Please select a lesson on the left before running code.");
                return;
            }

            OutputTextBox.Clear();
            OutputTextBox.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#D4D4D4")!;
            OutputTextBox.AppendText($"> Running code...{Environment.NewLine}");

            (string Output, string Error) result;
            if (_currentLesson.Id.StartsWith("py"))
                result = await _codeRunner.RunPythonAsync(Editor.Text);
            else
                result = await _codeRunner.RunCSharpAsync(Editor.Text);

            if (!string.IsNullOrEmpty(result.Error))
            {
                OutputTextBox.Foreground = System.Windows.Media.Brushes.OrangeRed;
                OutputTextBox.AppendText($"ERROR: {result.Error}{Environment.NewLine}");
            }
            
            if (!string.IsNullOrEmpty(result.Output))
                OutputTextBox.AppendText(result.Output);
        }

        private async void CheckAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLesson == null) return;

            OutputTextBox.Clear();
            OutputTextBox.AppendText($"> Validating challenge {_currentChallengeIndex + 1}...{Environment.NewLine}");

            var validation = await _validator.ValidateAsync(_currentLesson, _currentChallengeIndex, Editor.Text);
            
            if (validation.Success)
            {
                OutputTextBox.Foreground = System.Windows.Media.Brushes.LightGreen;
                OutputTextBox.AppendText($"SUCCESS: {validation.Message}{Environment.NewLine}");
                
                if (_currentChallengeIndex < _currentLesson.Challenges.Count - 1)
                {
                    _currentChallengeIndex++;
                    OutputTextBox.AppendText(App.CurrentLanguage == "sq" 
                        ? $"Duke kaluar te sfida {_currentChallengeIndex + 1}..." 
                        : $"Moving to challenge {_currentChallengeIndex + 1}...");
                    
                    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                    timer.Tick += (s, ev) => {
                        UpdateChallengeUI();
                        timer.Stop();
                    };
                    timer.Start();
                }
                else
                {
                    OutputTextBox.AppendText(App.CurrentLanguage == "sq" 
                        ? "URIME! I keni plotësuar të gjitha sfidat e këtij mësimi!" 
                        : "CONGRATULATIONS! You completed all challenges for this lesson!");
                    await _validator.SaveProgressAsync(_currentLesson.Id);
                    UpdateProgressStats();
                }
            }
            else
            {
                OutputTextBox.Foreground = System.Windows.Media.Brushes.OrangeRed;
                OutputTextBox.AppendText($"FAILED: {validation.Message}{Environment.NewLine}");
            }

            InitializeOutputTimer();
        }

        private async void AiHint_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLesson == null || _currentLesson.Challenges == null || _currentLesson.Challenges.Count == 0 || string.IsNullOrWhiteSpace(Editor.Text)) return;

            OutputTextBox.Clear();
            OutputTextBox.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#A8A8FF")!;
            OutputTextBox.AppendText($"> Asking Local AI for a hint...{Environment.NewLine}");

            var challenge = _currentLesson.Challenges[_currentChallengeIndex];
            
            // Disable button while loading
            AiHintButton.IsEnabled = false;
            
            string aiResponse = await _aiHintService.GetHintAsync(_currentLesson.Title, challenge.Exercise, Editor.Text, challenge.RequiredKeywords, _currentLesson.Id, _currentChallengeIndex);
            
            AiHintButton.IsEnabled = true;

            OutputTextBox.AppendText($"{aiResponse}{Environment.NewLine}");
            InitializeOutputTimer();
        }

        private void InitializeOutputTimer()
        {
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, ev) => {
                OutputTextBox.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#D4D4D4")!;
                timer.Stop();
            };
            timer.Start();
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LearnMenu_Click(object sender, RoutedEventArgs e)
        {
            LoadLessonsToUI();
            MessageBox.Show(App.CurrentLanguage == "sq" ? "Mësimet u rifreskuan!" : "Lessons refreshed!");
        }

        private void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new UI.SettingsWindow();
            settingsWin.Owner = this;
            settingsWin.ShowDialog();

            if (settingsWin.ReloadNeeded)
            {
                UpdateLocalization();
                LoadLessonsToUI();
                
                // Clear current lesson to avoid index errors
                _currentLesson = null;
                LessonTitleText.Text = App.CurrentLanguage == "sq" ? "Mirësevini!" : "Welcome!";
                LessonDescriptionText.Text = App.CurrentLanguage == "sq" ? "Ju lutem zgjidhni një mësim." : "Please select a lesson.";
                ExerciseText.Text = App.CurrentLanguage == "sq" ? "Gati?" : "Ready?";
            }
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new UI.ProfileWindow();
            profileWindow.Owner = this;
            profileWindow.ShowDialog();
        }
    }
}