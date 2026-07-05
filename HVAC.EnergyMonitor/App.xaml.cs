using System.IO;
using System.Windows;
using NLog;

namespace HVAC.EnergyMonitor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var configPath = "NLog.config";
        if (File.Exists(configPath))
        {
            LogManager.Setup().LoadConfigurationFromFile(configPath);
        }
        var bootstrapper = new Bootstrapper();
        bootstrapper.Run();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LogManager.Shutdown();
        base.OnExit(e);
    }
}
