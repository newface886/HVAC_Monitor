using HVAC.EnergyMonitor.Infrastructure.DbContext;
using HVAC.EnergyMonitor.Infrastructure.Helpers;
using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Models.Enums;
using HVAC.EnergyMonitor.Models.Events;
using HVAC.EnergyMonitor.Services.Cache;
using HVAC.EnergyMonitor.Services.Communication;
using HVAC.EnergyMonitor.Services.Storage;
using Microsoft.EntityFrameworkCore;
using NLog;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Acquisition;

public class DataAcquisitionService : IDataAcquisitionService
{
    private readonly AppDbContext _context;
    private readonly IPointValueCache _cache;
    private readonly IDataStorageService _storage;
    private readonly IEventAggregator _eventAggregator;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<int, ICommunicationService> _communicationServices = new();
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public bool IsRunning { get; private set; }

    public DataAcquisitionService(
        AppDbContext context,
        IPointValueCache cache,
        IDataStorageService storage,
        IEventAggregator eventAggregator)
    {
        _context = context;
        _cache = cache;
        _storage = storage;
        _eventAggregator = eventAggregator;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsRunning = true;
        _runningTask = RunAsync(_cts.Token);
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        if (_runningTask != null)
        {
            try { await _runningTask.WaitAsync(ct); }
            catch (OperationCanceledException) { }
        }

        foreach (var service in _communicationServices.Values)
        {
            try { await service.DisconnectAsync(ct); }
            catch { }
        }
        _communicationServices.Clear();
        IsRunning = false;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var devices = await _context.Devices
                    .Where(d => d.IsEnabled)
                    .Include(d => d.Points.Where(p => p.IsEnabled))
                    .ToListAsync(ct);

                foreach (var device in devices)
                {
                    await ProcessDeviceAsync(device, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[DataAcquisitionService] {Message}", ex.Message);
            }

            await Task.Delay(1000, ct);
        }
    }

    private async Task ProcessDeviceAsync(Device device, CancellationToken ct)
    {
        var service = GetOrCreateCommunicationService(device);
        if (!service.IsConnected)
        {
            await service.ConnectAsync(ct);
        }

        if (!service.IsConnected) return;

        var points = device.Points.ToList();
        foreach (var point in points)
        {
            try
            {
                ushort[] raw;
                if (point.FunctionCode == 3)
                    raw = await service.ReadHoldingRegistersAsync(device.SlaveAddress, point.RegisterAddress, GetRegisterCount(point.DataType), ct);
                else
                    raw = await service.ReadInputRegistersAsync(device.SlaveAddress, point.RegisterAddress, GetRegisterCount(point.DataType), ct);

                double engineeringValue = ConvertToEngineeringValue(raw, point);
                var cacheItem = new PointValueCacheItem(point.Id, engineeringValue, DateTime.Now, Quality.Good);
                _cache.SetValue(cacheItem);
                _eventAggregator.GetEvent<PointValueUpdatedEvent>().Publish(point.Id);

                if (point.StoreHistory)
                {
                    await _storage.EnqueueAsync(new Models.Entities.PointValue
                    {
                        PointId = point.Id,
                        Value = engineeringValue,
                        Timestamp = DateTime.Now,
                        Quality = Quality.Good
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "[ProcessDevice] Point {PointName}: {Message}", point.Name, ex.Message);
            }
        }
    }

    private ICommunicationService GetOrCreateCommunicationService(Device device)
    {
        if (_communicationServices.TryGetValue(device.Id, out var existing))
            return existing;

        ICommunicationService service = device.ProtocolType switch
        {
            ProtocolType.Simulator => new SimulatorCommunicationService(),
            ProtocolType.ModbusTCP => new ModbusTcpCommunicationService(),
            _ => new SimulatorCommunicationService()
        };

        _communicationServices[device.Id] = service;
        return service;
    }

    private static int GetRegisterCount(DataType dataType) => dataType switch
    {
        DataType.UShort or DataType.Short => 1,
        DataType.UInt or DataType.Int or DataType.Float => 2,
        _ => 1
    };

    private static double ConvertToEngineeringValue(ushort[] raw, Point point)
    {
        double rawValue = point.DataType switch
        {
            DataType.UShort => raw[0],
            DataType.Short => (short)raw[0],
            DataType.UInt => (uint)((raw[0] << 16) | raw[1]),
            DataType.Int => (raw[0] << 16) | raw[1],
            DataType.Float => ByteOrderConverter.ToFloat(raw[0], raw[1], point.ByteOrder),
            _ => raw[0]
        };
        return rawValue * point.Scale + point.Offset;
    }
}
