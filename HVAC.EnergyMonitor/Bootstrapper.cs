using HVAC.EnergyMonitor.Modules;
using HVAC.EnergyMonitor.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System.Windows;

namespace HVAC.EnergyMonitor;

public class Bootstrapper : PrismBootstrapper
{
    protected override DependencyObject CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<DashboardView>();
        containerRegistry.RegisterForNavigation<DeviceConfigView>();
        containerRegistry.RegisterForNavigation<PointConfigView>();
        containerRegistry.RegisterForNavigation<HistoryTrendView>();
        containerRegistry.RegisterForNavigation<AlarmView>();
        containerRegistry.RegisterForNavigation<EnergyReportView>();
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<CoreModule>();
    }
}
