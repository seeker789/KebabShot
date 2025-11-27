using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace KebabShot.Upload
{
    public class UploadService
    {
        private static readonly string LocalFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KebabShotUploads");

        public UploadService()
        {
            if (!Directory.Exists(LocalFolder))
                Directory.CreateDirectory(LocalFolder);
        }

        public async Task<string> UploadImageAsync(Bitmap bitmap)
        {
            // Simulate upload delay (optional)
            await Task.Delay(300);

            string fileName = $"kebabshot_{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
            string savePath = Path.Combine(LocalFolder, fileName);

            bitmap.Save(savePath, ImageFormat.Png);

            // Pretend this is a server URL
            string fakeUrl = $"https://fake.kebabshot.local/{fileName}";

            return fakeUrl;
        }
    }
}
