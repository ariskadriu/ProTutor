using System.Windows;
using System;
using ProgrammingTutor.Services;
using ProgrammingTutor.Core;
using System.Linq;

namespace ProgrammingTutor.UI
{
    public partial class ProfileWindow : Window
    {
        private readonly LeaderboardService _leaderboardService;
        private readonly LessonManager _lessonManager;
        private readonly JsonStorageService _storageService;

        public ProfileWindow()
        {
            InitializeComponent();
            
            _leaderboardService = new LeaderboardService();
            _lessonManager = new LessonManager();
            _storageService = new JsonStorageService();
            
            _lessonManager.LoadLessons();
            LoadData();
        }

        private async void LoadData()
        {
            UsernameText.Text = App.CurrentUser?.Username ?? "Guest";
            
            // Localization
            bool isAl = App.CurrentLanguage == "sq";
            ProfileHeader.Text = isAl ? "PROFILI IM" : "MY PROFILE";
            // Note: Tab headers are static in XAML for now, but could be dynamic
            
            // Load Progress
            string progressFile = $"progress_{App.CurrentUser?.Username ?? "Guest"}.json";
            var progress = await _storageService.LoadAsync<Models.Progress>(progressFile) ?? new Models.Progress();
            
            var allLessons = _lessonManager.GetLessons().Where(l => l.Id.EndsWith($"_{App.CurrentLanguage}")).ToList();
            int pyTotal = allLessons.Count(l => l.Id.StartsWith("py"));
            int pyDone = progress.CompletedLessons.Count(id => id.StartsWith("py") && id.EndsWith($"_{App.CurrentLanguage}"));
            
            int csTotal = allLessons.Count(l => l.Id.StartsWith("c"));
            int csDone = progress.CompletedLessons.Count(id => (id.StartsWith("c") || id.StartsWith("cs")) && id.EndsWith($"_{App.CurrentLanguage}"));

            PythonStats.Text = $"{pyDone}/{pyTotal}";
            CSharpStats.Text = $"{csDone}/{csTotal}";

            if (pyTotal > 0 && pyDone >= pyTotal) PythonCertBtn.Visibility = Visibility.Visible;
            if (csTotal > 0 && csDone >= csTotal) CSharpCertBtn.Visibility = Visibility.Visible;

            // Analytics Logic
            int totalAvailable = pyTotal + csTotal;
            int totalDone = pyDone + csDone;
            double percentage = totalAvailable > 0 ? ((double)totalDone / totalAvailable) * 100 : 0;
            
            // Assuming max width of 310 for the progress bar based on window width
            OverallProgressBar.Width = (percentage / 100.0) * 310;
            OverallProgressText.Text = $"{Math.Round(percentage)}%";
            LessonsConqueredText.Text = totalDone.ToString();
            
            bool isPythonActive = App.SelectedTrack == "py";
            ActiveTrackText.Text = isPythonActive ? "Python" : "C# (.NET)";
            ActiveTrackText.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(isPythonActive ? "#3776AB" : "#178600")!;

            // Localize additional text
            if (isAl)
            {
                AnalyticsHeader.Text = "ANAlITIKAT E MËSIMIT";
                OverallCompletionLabel.Text = "Përfundimi i Përgjithshëm";
                ConqueredLabel.Text = "Mësime të Kaluara";
                ActiveTrackLabel.Text = "Gjuha Aktive";
            }

            // Load Leaderboard
            LeaderboardList.ItemsSource = await _leaderboardService.GetTopUsersAsync();
        }

        private void ClaimCert_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string courseName)
            {
                string fullName = string.IsNullOrWhiteSpace(App.CurrentUser?.Name) ? (App.CurrentUser?.Username ?? "Guest") : $"{App.CurrentUser.Name} {App.CurrentUser.Surname}";
                var certWin = new CertificateWindow(fullName, courseName);
                certWin.Owner = this;
                certWin.ShowDialog();
            }
        }

        private void TestCert_Click(object sender, RoutedEventArgs e)
        {
            // Opens a preview certificate using the active track
            string track = App.SelectedTrack == "py" ? "Python" : "C#";
            string fullName = string.IsNullOrWhiteSpace(App.CurrentUser?.Name) ? (App.CurrentUser?.Username ?? "Guest") : $"{App.CurrentUser.Name} {App.CurrentUser.Surname}";
            var certWin = new CertificateWindow(fullName, track + " (Preview)");
            certWin.Owner = this;
            certWin.ShowDialog();
        }
    }
}
