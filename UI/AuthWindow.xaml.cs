using System.Windows;
using System.Windows.Controls;
using ProgrammingTutor.Services;
using ProgrammingTutor.Models;
using System.Threading.Tasks;

namespace ProgrammingTutor.UI
{
    public partial class AuthWindow : Window
    {
        private readonly AuthService _authService;
        private readonly LeaderboardService _leaderboardService;
        private bool _isRegisterMode = false;
        private string _selectedLang = "en";

        public AuthWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
            _leaderboardService = new LeaderboardService();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void SetLanguage_EN(object sender, RoutedEventArgs e) => TransitionToAuth("en");
        private void SetLanguage_SQ(object sender, RoutedEventArgs e) => TransitionToAuth("sq");

        private void TransitionToAuth(string lang)
        {
            _selectedLang = lang;
            LangSelectionPanel.Visibility = Visibility.Collapsed;
            AuthScrollParent.Visibility = Visibility.Visible;
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            if (_selectedLang == "sq")
            {
                SubtitleText.Text = "Udhëtimi juaj i kodimit fillon këtu";
                AuthHeader.Text = _isRegisterMode ? "REGJISTROHU" : "HYR";
                NameLabel.Text = "Emri";
                SurnameLabel.Text = "Mbiemri";
                UsernameLabel.Text = "Noçka (Pseudonimi)";
                PasswordLabel.Text = "Fjalëkalimi";
                MainAuthButton.Content = _isRegisterMode ? "Regjistrohu" : "Hyr";
                SwitchAuthButton.Content = _isRegisterMode ? "Keni një llogari? Hyr" : "Nuk keni llogari? Regjistrohu";
            }
            else
            {
                SubtitleText.Text = "Your coding journey starts here";
                AuthHeader.Text = _isRegisterMode ? "REGISTER" : "LOGIN";
                NameLabel.Text = "Name";
                SurnameLabel.Text = "Surname";
                UsernameLabel.Text = "Nickname";
                PasswordLabel.Text = "Password";
                MainAuthButton.Content = _isRegisterMode ? "Sign Up" : "Sign In";
                SwitchAuthButton.Content = _isRegisterMode ? "Already have an account? Login" : "Need an account? Register";
            }
        }

        private void SwitchAuth_Click(object sender, RoutedEventArgs e)
        {
            _isRegisterMode = !_isRegisterMode;
            RegisterFieldsPanel.Visibility = _isRegisterMode ? Visibility.Visible : Visibility.Collapsed;
            UpdateTexts();
        }

        private async void Auth_Click(object sender, RoutedEventArgs e)
        {
            string user = UsernameBox.Text;
            string pass = PasswordBox.Password;
            string name = NameBox.Text;
            string surname = SurnameBox.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || (_isRegisterMode && (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(surname))))
            {
                StatusText.Text = _selectedLang == "sq" ? "Ju lutem plotësoni të gjitha fushat" : "Please fill all fields";
                return;
            }

            (bool Success, string Message, User? User) result;
            if (_isRegisterMode)
            {
                result = await _authService.RegisterAsync(user, name, surname, pass);
                if (result.Success)
                {
                    // Auto-register on leaderboard at 0 points
                    await _leaderboardService.UpdateUserScoreAsync(user, 0);
                }
            }
            else
            {
                result = await _authService.LoginAsync(user, pass);
            }

            if (result.Success)
            {
                App.CurrentUser = result.User;
                App.CurrentLanguage = _selectedLang;
                TransitionToTrack();
            }
            else
            {
                StatusText.Text = result.Message;
            }
        }

        private void TransitionToTrack()
        {
            AuthScrollParent.Visibility = Visibility.Collapsed;
            TrackPanel.Visibility = Visibility.Visible;
            
            if (_selectedLang == "sq")
            {
                TrackHeader.Text = "Çfarë dëshironi të mësoni sot?";
                UserWelcomeText.Text = $"Mirësevini përsëri, {App.CurrentUser?.Username}!";
            }
            else
            {
                TrackHeader.Text = "What do you want to learn today?";
                UserWelcomeText.Text = $"Welcome back, {App.CurrentUser?.Username}!";
            }
        }

        private void SelectTrack_PY(object sender, RoutedEventArgs e) => LaunchMain("py");
        private void SelectTrack_CS(object sender, RoutedEventArgs e) => LaunchMain("cs");

        private void LaunchMain(string track)
        {
            App.SelectedTrack = track;
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void Legal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string type)
            {
                var legalWin = new LegalWindow(type);
                legalWin.Owner = this;
                legalWin.ShowDialog();
            }
        }
    }
}
