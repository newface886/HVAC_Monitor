using HVAC.EnergyMonitor.Infrastructure.DbContext;
using HVAC.EnergyMonitor.Services.Sync;
using Microsoft.EntityFrameworkCore;
using NLog;
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Modules;

public class CoreModule : IModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public void OnInitialized(IContainerProvider containerProvider)
    {
        try
        {
            using var context = containerProvider.Resolve<IDbContextFactory<AppDbContext>>().CreateDbContext();
            context.Database.EnsureCreated();
            SeedData(context);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[CoreModule] SQLite 数据库初始化失败: {Message}", ex.Message);
        }

        // SQL Server 镜像在后台任务启动，UI 不等待（避免 sync-over-async）
        if (containerProvider.IsRegistered<ISqlServerSchemaManager>() ||
            containerProvider.IsRegistered<IDataSyncService>())
        {
            _ = Task.Run(() => InitializeSqlServerAsync(containerProvider));
        }
    }

    private static async Task InitializeSqlServerAsync(IContainerProvider containerProvider)
    {
        try
        {
            if (containerProvider.IsRegistered<ISqlServerSchemaManager>())
            {
                var schemaManager = containerProvider.Resolve<ISqlServerSchemaManager>();
                await schemaManager.EnsureSchemaAsync().ConfigureAwait(false);
            }

            if (containerProvider.IsRegistered<IDataSyncService>())
            {
                var syncService = containerProvider.Resolve<IDataSyncService>();
                await syncService.StartAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[CoreModule] SQL Server 同步启动失败，应用以降级模式继续运行: {Message}", ex.Message);
        }
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 服务注册已移至 Bootstrapper.RegisterTypes
        // 原因：Prism 9 模块加载在 Shell 创建之后，导致 MainWindowViewModel 解析时服务未注册
    }

    private static void SeedData(Infrastructure.DbContext.AppDbContext context)
    {
        if (context.Devices.Any()) return;

        var device = new Models.Entities.Device
        {
            Name = "Simulator-Chiller-01",
            ProtocolType = Models.Enums.ProtocolType.Simulator,
            ScanIntervalMs = 1000,
            SlaveAddress = 1,
            IsEnabled = true
        };
        context.Devices.Add(device);
        context.SaveChanges();

        var points = new[]
        {
            new Models.Entities.Point { DeviceId = device.Id, Name = "冷冻水供水温度", FunctionCode = 3, RegisterAddress = 0, DataType = Models.Enums.DataType.UShort, Scale = 0.1, Offset = 0, Unit = "°C", HighLimit = 12, LowLimit = 5, StoreHistory = true },
            new Models.Entities.Point { DeviceId = device.Id, Name = "冷冻水回水温度", FunctionCode = 3, RegisterAddress = 1, DataType = Models.Enums.DataType.UShort, Scale = 0.1, Offset = 0, Unit = "°C", HighLimit = 15, LowLimit = 7, StoreHistory = true },
            new Models.Entities.Point { DeviceId = device.Id, Name = "冷机功率", FunctionCode = 3, RegisterAddress = 2, DataType = Models.Enums.DataType.UShort, Scale = 1, Offset = 0, Unit = "kW", HighLimit = 500, LowLimit = 0, StoreHistory = true },
            new Models.Entities.Point { DeviceId = device.Id, Name = "冷却塔风机频率", FunctionCode = 3, RegisterAddress = 3, DataType = Models.Enums.DataType.UShort, Scale = 0.1, Offset = 0, Unit = "Hz", HighLimit = 50, LowLimit = 0, StoreHistory = true }
        };
        context.Points.AddRange(points);
        context.SaveChanges();
    }
}
