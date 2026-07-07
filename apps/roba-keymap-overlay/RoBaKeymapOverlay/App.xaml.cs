using System.Windows;

namespace RoBaKeymapOverlay;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new OverlayWindow();
        MainWindow = window;
        window.Show();
    }
}
