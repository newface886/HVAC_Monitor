using System.Windows;
using NLog;

namespace HVAC.EnergyMonitor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        var bootstrapper = new Bootstrapper();
        bootstrapper.Run();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LogManager.Shutdown();
        base.OnExit(e);
    }
}
