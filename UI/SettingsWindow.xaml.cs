using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace ProgrammingTutor.UI
{
    public partial class SettingsWindow : Window
    {
        public bool ReloadNeeded { get; private set; } = false;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
            UpdateLocalization();
        }

        private void LoadCurrentSettings()
        {
            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if (item.Tag.ToString() == App.CurrentLanguage)
                {
                    LanguageCombo.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in TrackCombo.Items)
            {
                if (item.Tag.ToString() == App.SelectedTrack)
                {
                    TrackCombo.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in ThemeCombo.Items)
            {
                if (item.Tag.ToString() == App.CurrentThemeHex)
                {
                    ThemeCombo.SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateLocalization()
        {
            bool isAl = App.CurrentLanguage == "sq";
            SettingsHeader.Text = isAl ? "CILËSIMET" : "SETTINGS";
            LangLabel.Text = isAl ? "Gjuha e Aplikacionit" : "App Language";
            TrackLabel.Text = isAl ? "Rruga e Mësimit" : "Learning Track";
            ThemeLabel.Text = isAl ? "Ngjyra e Aplikacionit" : "App Theme Color";
            PrivacyBtn.Content = isAl ? "Politika e Privatësisë" : "Privacy Policy";
            TermsBtn.Content = isAl ? "Kushtet e Përdorimit" : "Terms";
            SaveButton.Content = isAl ? "RUAJ & RIFRESKO" : "SAVE & RELOAD";

            if (!_isAdminUnlocked)
            {
                AdminHeader.Text = isAl ? "SHTOJCË ADMIN (Kërkon PIN)" : "ADMIN PLUGINS (Requires PIN)";
                AdminActionBtn.Content = isAl ? "ZHBLLOKO" : "UNLOCK";
            }
            else
            {
                AdminHeader.Text = isAl ? "SHTOJCË ADMIN (Zhbllokuar)" : "ADMIN PLUGINS (Unlocked)";
                AdminActionBtn.Content = isAl ? "IMPORTO JSON" : "IMPORT JSON";
            }
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

        private bool _isAdminUnlocked = false;

        private void AdminAction_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminUnlocked)
            {
                if (PinBox.Password == "ARIS123")
                {
                    _isAdminUnlocked = true;
                    AdminActionBtn.Content = "IMPORT JSON";
                    AdminActionBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 175, 55)); // Gold
                    AdminActionBtn.Foreground = System.Windows.Media.Brushes.Black;
                    PinBox.Visibility = Visibility.Collapsed;
                    AdminHeader.Text = "ADMIN PLUGINS (Unlocked)";
                    AdminHeader.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    MessageBox.Show(App.CurrentLanguage == "sq" ? "PIN i pasaktë!" : "Incorrect PIN!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Import logic
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON Lesson Files (*.json)|*.json",
                    Title = "Import Authorized Plugin"
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        string targetDir = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Content", "lessons", "plugins");
                        if (!System.IO.Directory.Exists(targetDir))
                            System.IO.Directory.CreateDirectory(targetDir);

                        string targetPath = System.IO.Path.Combine(targetDir, System.IO.Path.GetFileName(dialog.FileName));
                        System.IO.File.Copy(dialog.FileName, targetPath, true);
                        
                        MessageBox.Show(App.CurrentLanguage == "sq" ? "Shtojca u importua me sukses!" : "Plugin successfully imported!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        ReloadNeeded = true;
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageCombo.SelectedItem is ComboBoxItem langItem)
                App.CurrentLanguage = langItem.Tag.ToString()!;

            if (TrackCombo.SelectedItem is ComboBoxItem trackItem)
                App.SelectedTrack = trackItem.Tag.ToString()!;

            if (ThemeCombo.SelectedItem is ComboBoxItem themeItem)
            {
                App.ApplyTheme(themeItem.Tag.ToString()!);
            }

            ReloadNeeded = true;
            this.Close();
        }
    }
}
