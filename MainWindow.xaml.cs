using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Linq;
using System.Collections.Generic;
using ProgrammingTutor.Core;
using ProgrammingTutor.Models;
using ProgrammingTutor.Services;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ProgrammingTutor
{
    public class ChatMessageUiModel
    {
        public string Text { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public HorizontalAlignment Alignment { get; set; }
        public Brush Color { get; set; } = Brushes.Transparent;
    }

    public partial class MainWindow : Window
    {
        private readonly LessonManager _lessonManager;
        private readonly JsonStorageService _storageService;
        private readonly CodeRunner _codeRunner;
        private readonly ExerciseValidator _validator;
        private readonly AIHintService _aiHintService;
        private Lesson? _currentLesson;
        private int _currentChallengeIndex = 0;
        private int _lastOutputIndex = 0;
        private bool _isRunning = false;
        private string _lastTerminalError = string.Empty;
        public ObservableCollection<ChatMessageUiModel> AiChatMessages { get; set; } = new ObservableCollection<ChatMessageUiModel>();

        public MainWindow()
        {
            InitializeComponent();
            
            _lessonManager = new LessonManager();
            _storageService = new JsonStorageService();
            _codeRunner = new CodeRunner();
            _validator = new ExerciseValidator(_codeRunner, _storageService);
            _aiHintService = new AIHintService();
            
            UserText.Text = App.CurrentUser?.Username ?? "Guest";
            // ApiKey update removed for Local AI
            UpdateLocalization();

            // Set default highlighting
            Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            Editor.Options.ConvertTabsToSpaces = true;
            Editor.Options.IndentationSize = 4;

            LoadLessonsToUI();

            // Setup CodeRunner events
            _codeRunner.OutputReceived += (data) => AppendToTerminal(data);
            _codeRunner.ErrorReceived += (data) => 
            {
                _lastTerminalError = data;
                AppendToTerminal(data, true);
            };
            _codeRunner.ProcessExited += (code) => {
                Dispatcher.Invoke(() => {
                    _isRunning = false;
                    StopButton.Visibility = Visibility.Collapsed;
                    RunButton.IsEnabled = true;
                    AppendToTerminal($"> Process exited with code {code}{Environment.NewLine}");
                });
            };

            AiChatListBox.ItemsSource = AiChatMessages;
            AiChatInput.Text = App.CurrentLanguage == "sq" ? "Pyesni ARIS AI këtu..." : "Ask ARIS AI anything...";
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
            
            LearnMenu.Content = isAl ? "MËSO" : "LEARN";
            SettingsMenu.Content = isAl ? "CILËSIMET" : "SETTINGS";
        }

        private void LoadLessonsToUI()
        {
            _lessonManager.LoadLessons();
            LessonTreeView.Items.Clear();

            string trackFilter = App.SelectedTrack;
            string langName = trackFilter == "py" ? "PYTHON" : "C# (.NET)";
            
            var rootItem = new TreeViewItem 
            { 
                Header = langName,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#3B82F6")!,
                FontWeight = FontWeights.Bold,
                IsExpanded = true
            };

            // Define Modules
            var modules = new Dictionary<string, TreeViewItem>();
            string[] moduleNames = trackFilter == "py" 
                ? new[] { "Basics", "Functions", "OOP", "Data Structures", "Advanced" }
                : new[] { "Basics", "OOP", "Advanced" };

            foreach (var mod in moduleNames)
            {
                var modItem = new TreeViewItem 
                { 
                    Header = mod, 
                    Foreground = Brushes.Gray,
                    FontWeight = FontWeights.SemiBold,
                    IsExpanded = false
                };
                modules[mod] = modItem;
                rootItem.Items.Add(modItem);
            }

            var lessons = _lessonManager.GetLessonsByCategory(trackFilter);
            foreach (var lesson in lessons)
            {
                if (lesson.Id.EndsWith($"_{App.CurrentLanguage}"))
                {
                    var lessonItem = new TreeViewItem 
                    { 
                        Header = lesson.Title,
                        Tag = lesson.Id,
                        Foreground = (Brush)new BrushConverter().ConvertFromString("#E5E7EB")!
                    };

                    string module = GetModuleForLesson(lesson.Id, trackFilter);
                    if (modules.ContainsKey(module))
                        modules[module].Items.Add(lessonItem);
                    else
                        modules["Advanced"].Items.Add(lessonItem);
                }
            }

            // Remove empty modules
            for (int i = rootItem.Items.Count - 1; i >= 0; i--)
            {
                if ((rootItem.Items[i] as TreeViewItem)?.Items.Count == 0)
                    rootItem.Items.RemoveAt(i);
            }

            LessonTreeView.Items.Add(rootItem);
            UpdateProgressStats();
        }

        private string GetModuleForLesson(string id, string track)
        {
            id = id.ToLower();
            if (track == "py")
            {
                if (id.Contains("functions") || id.Contains("lambda") || id.Contains("scope")) return "Functions";
                if (id.Contains("classes") || id.Contains("inheritance") || id.Contains("polymorphism") || id.Contains("oop")) return "OOP";
                if (id.Contains("lists") || id.Contains("tuple") || id.Contains("sets") || id.Contains("dict") || id.Contains("dsa_stack") || id.Contains("dsa_queue")) return "Data Structures";
                if (id.Contains("basics") || id.Contains("syntax") || id.Contains("variables") || id.Contains("print") || id.Contains("operators") || id.Contains("if") || id.Contains("loops") || id.Contains("numbers") || id.Contains("bool") || id.Contains("casting") || id.Contains("strings")) return "Basics";
                return "Advanced";
            }
            else // C#
            {
                if (id.Contains("classes") || id.Contains("interfaces") || id.Contains("inheritance") || id.Contains("polymorphism") || id.Contains("abstraction") || id.Contains("constructors") || id.Contains("access_modifiers")) return "OOP";
                if (id.Contains("basics") || id.Contains("hello") || id.Contains("variables") || id.Contains("data_types") || id.Contains("operators") || id.Contains("casting") || id.Contains("input") || id.Contains("bool")) return "Basics";
                return "Advanced";
            }
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
                
                if (MainProgressBar != null)
                {
                    MainProgressBar.Maximum = total > 0 ? total : 100;
                    MainProgressBar.Value = completed;
                }
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
            _lastOutputIndex = OutputTextBox.Text.Length;
            _lastTerminalError = string.Empty; // Clear old error

            _isRunning = true;
            RunButton.IsEnabled = false;
            StopButton.Visibility = Visibility.Visible;

            if (_currentLesson.Id.StartsWith("py"))
                await _codeRunner.RunPythonAsync(Editor.Text);
            else
                await _codeRunner.RunCSharpAsync(Editor.Text);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _codeRunner.Stop();
        }

        private void AppendToTerminal(string text, bool isError = false)
        {
            Dispatcher.Invoke(() => {
                if (isError) OutputTextBox.Foreground = System.Windows.Media.Brushes.OrangeRed;
                OutputTextBox.AppendText(text);
                OutputTextBox.ScrollToEnd();
                _lastOutputIndex = OutputTextBox.Text.Length;
            });
        }

        private void OutputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isRunning) return;

            // Ensure caret is at the end if user tries to type in history
            if (OutputTextBox.SelectionStart < _lastOutputIndex)
            {
                if (e.Key != Key.Right && e.Key != Key.Down && e.Key != Key.PageDown && e.Key != Key.End)
                {
                    OutputTextBox.SelectionStart = OutputTextBox.Text.Length;
                }
            }

            // Prevent backspacing into history
            if (e.Key == Key.Back && OutputTextBox.SelectionStart <= _lastOutputIndex)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                int start = _lastOutputIndex;
                int length = OutputTextBox.Text.Length - start;
                string input = length > 0 ? OutputTextBox.Text.Substring(start, length) : "";
                
                _codeRunner.SendInput(input);
                
                // Let the Enter key be processed to add the newline in the UI
                // We'll update _lastOutputIndex in a post-process
                Dispatcher.BeginInvoke(new Action(() => {
                    OutputTextBox.ScrollToEnd();
                    _lastOutputIndex = OutputTextBox.Text.Length;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
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

            // Switch to Chat Tab
            AiChatTab.IsSelected = true;

            AddChatMessage(App.CurrentLanguage == "sq" ? "Më jep një ndihmë për këtë sfidë." : "Give me a hint for this challenge.", "User");

            var challenge = _currentLesson.Challenges[_currentChallengeIndex];
            AiHintButton.IsEnabled = false;
            
            string aiResponse = await _aiHintService.GetHintAsync(_currentLesson.Title, challenge.Exercise, Editor.Text, challenge.RequiredKeywords, _currentLesson.Id, _currentChallengeIndex, _lastTerminalError);
            
            AiHintButton.IsEnabled = true;
            AddChatMessage(aiResponse, "Aris");
        }

        private async void AiChatSend_Click(object sender, RoutedEventArgs e)
        {
            string message = AiChatInput.Text;
            if (string.IsNullOrWhiteSpace(message) || message.Contains("...")) return;

            AiChatInput.Clear();
            AddChatMessage(message, "User");

            string response = await _aiHintService.GetChatResponseAsync(message);
            AddChatMessage(response, "Aris");
        }

        private void AiChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AiChatSend_Click(sender, e);
            }
        }

        private void AiChatInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AiChatInput.Text.Contains("..."))
                AiChatInput.Clear();
        }

        private void AddChatMessage(string text, string sender)
        {
            bool isAris = sender == "Aris";
            var msg = new ChatMessageUiModel
            {
                Text = text,
                Role = sender,
                Alignment = isAris ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                Color = (Brush)new BrushConverter().ConvertFromString(isAris ? "#1E1E2E" : "#3B82F6")!
            };
            
            AiChatMessages.Add(msg);
            AiChatListBox.ScrollIntoView(msg);
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
                // ApiKey update removed for Local AI
                
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