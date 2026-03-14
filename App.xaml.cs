using System.Windows;
using System;

namespace ProgrammingTutor
{
    public partial class App : Application
    {
        public static Models.User? CurrentUser { get; set; }
        public static string CurrentLanguage { get; set; } = "en";
        public static string SelectedTrack { get; set; } = "py";
        public static string CurrentThemeHex { get; set; } = "#007ACC";

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ev) => 
            {
                MessageBox.Show($"FATAL ERROR: {ev.ExceptionObject}", "Crash Report", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            base.OnStartup(e);
            try 
            {
                ApplyTheme(CurrentThemeHex);
                var authWindow = new UI.AuthWindow();
                authWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"STARTUP ERROR: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Startup Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        public static void ApplyTheme(string hexColor)
        {
            CurrentThemeHex = hexColor;
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
            
            ResourceDictionary? modernStyles = null;
            foreach (var dict in Application.Current.Resources.MergedDictionaries)
            {
                if (dict.Source != null && dict.Source.ToString().Contains("ModernStyles.xaml"))
                {
                    modernStyles = dict;
                    break;
                }
            }

            var targetDict = modernStyles ?? Application.Current.Resources;

            targetDict["AccentColor"] = color;
            targetDict["AccentBrush"] = new System.Windows.Media.SolidColorBrush(color);
            
            // Also update the PremiumGradient
            var gradient = new System.Windows.Media.LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 1);
            gradient.GradientStops.Add(new System.Windows.Media.GradientStop(color, 0));
            
            // Lighter variant for gradient end
            var lighterColor = System.Windows.Media.Color.FromRgb(
                (byte)Math.Min(255, color.R + 40),
                (byte)Math.Min(255, color.G + 40),
                (byte)Math.Min(255, color.B + 40));
            gradient.GradientStops.Add(new System.Windows.Media.GradientStop(lighterColor, 1));
            
            targetDict["PremiumGradient"] = gradient;
        }
    }
}
