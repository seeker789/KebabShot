using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace KebabShot.Utils
{
    public class GlobalHotkey : IDisposable
    {
        private readonly int _id;
        private readonly IntPtr _handle;

        public event EventHandler? HotkeyPressed;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        const uint MOD_NONE = 0x0000;
        const uint VK_SNAPSHOT = 0x2C; // PrintScreen key

        public GlobalHotkey(Window window)
        {
            _id = GetHashCode();
            _handle = new WindowInteropHelper(window).EnsureHandle();

            HwndSource source = HwndSource.FromHwnd(_handle)!;
            source.AddHook(WndProc);

            RegisterHotKey(_handle, _id, MOD_NONE, VK_SNAPSHOT);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotKey(_handle, _id);
        }
    }
}
