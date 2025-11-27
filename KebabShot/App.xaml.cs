using System.Windows;

namespace KebabShot
{
    public partial class App : Application
    {
        private Tray.TrayService? _tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _tray = new Tray.TrayService();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray?.Dispose();
            base.OnExit(e);
        }
    }
}
