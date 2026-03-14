using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProgrammingTutor.UI
{
    public partial class CertificateWindow : Window
    {
        public CertificateWindow(string fullName, string courseName)
        {
            InitializeComponent();
            
            RecipientName.Text = fullName.ToUpper();
            
            bool isPreview = courseName.Contains("Preview", StringComparison.OrdinalIgnoreCase);
            if (isPreview)
            {
                SavePdfBtn.Visibility = Visibility.Collapsed;
                CourseNameText.Text = $"{(courseName.StartsWith("py", StringComparison.OrdinalIgnoreCase) || courseName.StartsWith("Python", StringComparison.OrdinalIgnoreCase) ? "Python" : "C# (.NET)")} Programming Masterclass";
            }
            else
            {
                CourseNameText.Text = $"{(courseName == "py" || courseName.Equals("Python", StringComparison.OrdinalIgnoreCase) ? "Python" : "C# (.NET)")} Programming Masterclass";
            }
            
            DateText.Text = DateTime.Now.ToString("MMMM dd, yyyy");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = $"{RecipientName.Text}_Certificate.png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Update layout to ensure accurate rendering
                    CertificateGrid.UpdateLayout();

                    // Create RenderTargetBitmap
                    int width = (int)CertificateGrid.ActualWidth;
                    int height = (int)CertificateGrid.ActualHeight;
                    
                    if (width == 0 || height == 0)
                    {
                        // Fallbacks if layout hasn't properly measured yet
                        width = 800;
                        height = 565;
                        CertificateGrid.Measure(new Size(width, height));
                        CertificateGrid.Arrange(new Rect(0, 0, width, height));
                    }

                    RenderTargetBitmap renderTarget = new RenderTargetBitmap(
                        width, height, 96, 96, PixelFormats.Pbgra32);

                    renderTarget.Render(CertificateGrid);

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show("Certificate successfully saved as PNG!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving certificate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
