using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KebabShot.Editor
{
    public partial class CropPreviewWindow : Window
    {
        private Bitmap _bitmap;

        public CropPreviewWindow(Bitmap bmp)
        {
            InitializeComponent();
            _bitmap = bmp;
            PreviewImage.Source = ToImageSource(_bitmap);
        }

        private BitmapImage ToImageSource(Bitmap bmp)
        {
            using MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();

            return img;
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(PreviewImage.Source as BitmapSource);
            MessageBox.Show("Image copied to clipboard!");
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"kebabshot_{DateTime.Now:HH_mm_ss}.png"
            );

            _bitmap.Save(path);
            MessageBox.Show($"Saved to:\n{path}");
        }

        private async void UploadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uploader = new Upload.UploadService();
                string url = await uploader.UploadImageAsync(_bitmap);

                Clipboard.SetText(url);

                MessageBox.Show($"Uploaded!\nURL copied:\n{url}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Upload failed: " + ex.Message);
            }
        }
    }
}
