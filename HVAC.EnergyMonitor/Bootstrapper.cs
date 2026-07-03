using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using DialogServiceImpl = HVAC.EnergyMonitor.Services.Dialog.DialogService;
using HVAC.EnergyMonitor.Infrastructure.DbContext;
using HVAC.EnergyMonitor.Infrastructure.Repository;
using HVAC.EnergyMonitor.Modules;
using HVAC.EnergyMonitor.Services.Acquisition;
using HVAC.EnergyMonitor.Services.Alarm;
using HVAC.EnergyMonitor.Services.Cache;
using HVAC.EnergyMonitor.Services.Common;
using HVAC.EnergyMonitor.Services.Communication;
using HVAC.EnergyMonitor.Services.Report;
using HVAC.EnergyMonitor.Services.Storage;
using HVAC.EnergyMonitor.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
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
        // 读取配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        containerRegistry.RegisterInstance<IConfiguration>(configuration);

        // 数据库（必须在 Shell 创建前注册，因为 MainWindowViewModel 依赖 IDialogService）
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=hvac_energy_monitor.db";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        containerRegistry.RegisterInstance(options);
        containerRegistry.RegisterSingleton<IDbContextFactory<AppDbContext>, AppDbContextFactory>();

        // Repository / UnitOfWork - Transient to avoid concurrent DbContext usage
        containerRegistry.Register<IUnitOfWork, UnitOfWork>();

        // Dialog
        containerRegistry.RegisterSingleton<DialogServiceInterface, DialogServiceImpl>();

        // Dispatcher
        containerRegistry.RegisterSingleton<IDispatcherService, DispatcherService>();

        // 服务
        containerRegistry.RegisterSingleton<ICommunicationServiceFactory, CommunicationServiceFactory>();
        containerRegistry.RegisterSingleton<IPointValueCache, PointValueCache>();
        containerRegistry.RegisterSingleton<IDataStorageService, DataStorageService>();
        containerRegistry.RegisterSingleton<IAlarmService, AlarmService>();
        containerRegistry.RegisterSingleton<IEnergyReportService, EnergyReportService>();
        containerRegistry.RegisterSingleton<IDataAcquisitionService, DataAcquisitionService>();

        // 导航注册
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
