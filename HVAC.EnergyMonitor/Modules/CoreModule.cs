using HVAC.EnergyMonitor.Infrastructure.DbContext;
using HVAC.EnergyMonitor.Infrastructure.Repository;
using HVAC.EnergyMonitor.Services.Acquisition;
using HVAC.EnergyMonitor.Services.Alarm;
using HVAC.EnergyMonitor.Services.Cache;
using HVAC.EnergyMonitor.Services.Communication;
using HVAC.EnergyMonitor.Services.Report;
using HVAC.EnergyMonitor.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Prism.Ioc;
using Prism.Modularity;

namespace HVAC.EnergyMonitor.Modules;

public class CoreModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // DbContext
        var connectionString = "Data Source=hvac_energy_monitor.db";
        containerRegistry.RegisterSingleton<AppDbContext>(() =>
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .Options;
            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            SeedData(context);
            return context;
        });

        // Repository / UnitOfWork
        containerRegistry.RegisterSingleton<IUnitOfWork, UnitOfWork>();

        // Services
        containerRegistry.RegisterSingleton<IPointValueCache, PointValueCache>();
        containerRegistry.RegisterSingleton<IDataStorageService, DataStorageService>();
        containerRegistry.RegisterSingleton<IAlarmService, AlarmService>();
        containerRegistry.RegisterSingleton<IEnergyReportService, EnergyReportService>();
        containerRegistry.RegisterSingleton<IDataAcquisitionService, DataAcquisitionService>();
    }

    private static void SeedData(AppDbContext context)
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
