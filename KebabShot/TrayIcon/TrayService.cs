using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using KebabShot.Utils;
using KebabShot.Capture;
using KebabShot.Editor;

namespace KebabShot.Tray
{
    public class TrayService : IDisposable
    {
        private readonly TaskbarIcon _icon;

        private readonly HotkeyWindow _hotkeyWindow;
        private readonly GlobalHotkey _hotkey;

        public TrayService()
        {
            //
            // 1) Create invisible window for hotkey processing
            //
            _hotkeyWindow = new HotkeyWindow();
            _hotkeyWindow.Show();         // Must show to create a window handle
            _hotkeyWindow.Hide();         // Hide immediately so user never sees it

            //
            // 2) Register global PrintScreen hotkey
            //
            _hotkey = new GlobalHotkey(_hotkeyWindow);
            _hotkey.HotkeyPressed += OnHotkeyPressed;

            //
            // 3) Setup tray icon
            //
            _icon = new TaskbarIcon();
            _icon.Icon = System.Drawing.SystemIcons.Application;
            _icon.ToolTipText = "KebabShot";
            _icon.Visibility = Visibility.Visible;


            var menu = new System.Windows.Controls.ContextMenu();
            menu.Items.Add(CreateItem("Take Screenshot", OnScreenshot));
            menu.Items.Add(CreateItem("Exit", OnExit));
            _icon.ContextMenu = menu;
        }

        // ------------------------------------------------------------
        // Helper for menu items
        // ------------------------------------------------------------
        private System.Windows.Controls.MenuItem CreateItem(string text, RoutedEventHandler action)
        {
            var item = new System.Windows.Controls.MenuItem { Header = text };
            item.Click += action;
            return item;
        }

        // ------------------------------------------------------------
        // GLOBAL HOTKEY → PrintScreen
        // ------------------------------------------------------------
        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            TriggerScreenshot();
        }

        // ------------------------------------------------------------
        // MENU ITEM → Take Screenshot
        // ------------------------------------------------------------
        private void OnScreenshot(object sender, RoutedEventArgs e)
        {
            TriggerScreenshot();
        }

        // ------------------------------------------------------------
        // Screenshot workflow
        // ------------------------------------------------------------
        private void TriggerScreenshot()
        {
            // Full capture
            var full = CaptureService.CaptureFullScreen();

            // Overlay selection
            var overlay = new ScreenOverlayWindow(full);
            overlay.ShowDialog();

            // If user selected a region → preview window
            if (overlay.DialogResult == true && overlay.CroppedBitmap != null)
            {
                var preview = new CropPreviewWindow(overlay.CroppedBitmap);
                preview.Show();
            }
        }

        // ------------------------------------------------------------
        // EXIT
        // ------------------------------------------------------------
        private void OnExit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ------------------------------------------------------------
        // CLEANUP
        // ------------------------------------------------------------
        public void Dispose()
        {
            _hotkey.HotkeyPressed -= OnHotkeyPressed;
            _hotkey.Dispose();

            _hotkeyWindow.Close();
            _icon.Dispose();
        }
    }
}
