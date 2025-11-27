using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

// ALIAS THE DRAWING TYPES
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingRectangle = System.Drawing.Rectangle;
using Graphics = System.Drawing.Graphics;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace KebabShot.Capture
{
    public partial class ScreenOverlayWindow : Window
    {
        private Point _startPoint;
        private bool _isDragging = false;

        public DrawingBitmap? CapturedBitmap { get; private set; }
        public DrawingBitmap? CroppedBitmap { get; private set; }

        public ScreenOverlayWindow(DrawingBitmap screenshot)
        {
            InitializeComponent();
            CapturedBitmap = screenshot;
            BackgroundImage.Source = BitmapToImageSource(screenshot);
        }

        private BitmapImage BitmapToImageSource(DrawingBitmap bmp)
        {
            using MemoryStream memory = new MemoryStream();
            bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = memory;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            return img;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(OverlayCanvas);
            _isDragging = true;

            SelectionRectangle.Visibility = Visibility.Visible;

            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point pos = e.GetPosition(OverlayCanvas);

            double x = Math.Min(pos.X, _startPoint.X);
            double y = Math.Min(pos.Y, _startPoint.Y);
            double w = Math.Abs(pos.X - _startPoint.X);
            double h = Math.Abs(pos.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = w;
            SelectionRectangle.Height = h;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            _isDragging = false;

            double x = Canvas.GetLeft(SelectionRectangle);
            double y = Canvas.GetTop(SelectionRectangle);
            double w = SelectionRectangle.Width;
            double h = SelectionRectangle.Height;

            CroppedBitmap = new DrawingBitmap((int)w, (int)h);

            using (Graphics g = Graphics.FromImage(CroppedBitmap))
            {
                g.DrawImage(
                    CapturedBitmap!,
                    new DrawingRectangle(0, 0, (int)w, (int)h),
                    new DrawingRectangle((int)x, (int)y, (int)w, (int)h),
                    System.Drawing.GraphicsUnit.Pixel);
            }

            DialogResult = true;
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
