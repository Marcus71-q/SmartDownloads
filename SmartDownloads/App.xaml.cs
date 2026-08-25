using System.Threading;
using System.Windows;

namespace SmartDownloads;

public partial class App : System.Windows.Application
{
    private Mutex? _instanceMutex;
    private bool _ownsMutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(true, "Local\\SmartDownloads.SingleInstance", out _ownsMutex);
        if (!_ownsMutex) { Shutdown(); return; }
        base.OnStartup(e);
        var background = e.Args.Any(x => x.Equals("--background", StringComparison.OrdinalIgnoreCase));
        var window = new MainWindow(background);
        MainWindow = window;
        if (!background || !window.IsConfigured) window.Show();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex) _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
