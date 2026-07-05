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
using HVAC.EnergyMonitor.Services.Sync;
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
        // SQLite 工厂（保持原有 IDbContextFactory<AppDbContext> 注册）
        var sqliteConnection = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=hvac_energy_monitor.db";
        var sqliteOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(sqliteConnection)
            .Options;
        var sqliteFactory = new AppDbContextFactory(sqliteOptions);
        containerRegistry.RegisterInstance(sqliteOptions);
        containerRegistry.RegisterInstance<IDbContextFactory<AppDbContext>>(sqliteFactory);

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

        // SQL Server 镜像（仅当配置了 SqlServerConnection 时启用）
        var sqlServerConnection = configuration.GetConnectionString("SqlServerConnection");
        if (!string.IsNullOrWhiteSpace(sqlServerConnection))
        {
            // 关键：不注册为 IDbContextFactory<AppDbContext>，避免与 SQLite 工厂冲突
            // 仅作为普通对象传给需要的服务
            var sqlServerFactory = new SqlServerDbContextFactory(sqlServerConnection);
            containerRegistry.RegisterInstance<ISqlServerSchemaManager>(
                new SqlServerSchemaManager(sqlServerFactory));
        }
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<CoreModule>();
    }
}
