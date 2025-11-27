using System.Drawing;
using System.Drawing.Imaging;

namespace KebabShot.Capture
{
    public static class CaptureService
    {
        public static Bitmap CaptureFullScreen()
        {
            // USE FULLY QUALIFIED NAME (NO USING System.Windows.Forms)
            var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            }

            return screenshot;
        }
    }
}
