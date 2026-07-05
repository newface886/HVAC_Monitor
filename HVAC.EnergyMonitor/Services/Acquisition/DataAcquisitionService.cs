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

public class DataAcquisitionService : IDataAcquisitionService, IAsyncDisposable, IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IPointValueCache _cache;
    private readonly IDataStorageService _storage;
    private readonly IEventAggregator _eventAggregator;
    private readonly ICommunicationServiceFactory _communicationServiceFactory;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<int, ICommunicationService> _communicationServices = new();
    private CancellationTokenSource? _cts;
    private Task? _runningTask;
    private bool _disposed;

    public bool IsRunning { get; private set; }

    public DataAcquisitionService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IPointValueCache cache,
        IDataStorageService storage,
        IEventAggregator eventAggregator,
        ICommunicationServiceFactory communicationServiceFactory)
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _storage = storage;
        _eventAggregator = eventAggregator;
        _communicationServiceFactory = communicationServiceFactory;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsRunning = true;
        _runningTask = RunAsync(_cts.Token);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        if (_runningTask != null)
        {
            try { await _runningTask.WaitAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        foreach (var service in _communicationServices.Values)
        {
            try { await service.DisconnectAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { Logger.Warn(ex, "[DataAcquisitionService] DisconnectAsync failed during Stop"); }
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
                using var context = _dbContextFactory.CreateDbContext();
                var devices = await context.Devices
                    .AsNoTracking()
                    .Where(d => d.IsEnabled)
                    .Include(d => d.Points.Where(p => p.IsEnabled))
                    .ToListAsync(ct).ConfigureAwait(false);

                foreach (var device in devices)
                {
                    await ProcessDeviceAsync(device, ct).ConfigureAwait(false);
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

            try
            {
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessDeviceAsync(Device device, CancellationToken ct)
    {
        var service = GetOrCreateCommunicationService(device);
        if (!service.IsConnected)
        {
            await service.ConnectAsync(ct).ConfigureAwait(false);
        }

        if (!service.IsConnected) return;

        var points = device.Points.ToList();
        foreach (var point in points)
        {
            try
            {
                ushort[] raw;
                if (point.FunctionCode == 3)
                    raw = await service.ReadHoldingRegistersAsync(device.SlaveAddress, point.RegisterAddress, GetRegisterCount(point.DataType), ct).ConfigureAwait(false);
                else
                    raw = await service.ReadInputRegistersAsync(device.SlaveAddress, point.RegisterAddress, GetRegisterCount(point.DataType), ct).ConfigureAwait(false);

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
                    }).ConfigureAwait(false);
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

        var service = _communicationServiceFactory.Create(device.ProtocolType, device);
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[DataAcquisitionService] DisposeAsync StopAsync failed: {Message}", ex.Message);
        }

        _cts?.Dispose();
        _runningTask?.Dispose();

        foreach (var service in _communicationServices.Values)
        {
            if (service is IAsyncDisposable asyncDisposable)
            {
                try { await asyncDisposable.DisposeAsync().ConfigureAwait(false); }
                catch (Exception ex) { Logger.Warn(ex, "[DataAcquisitionService] Communication service async dispose failed"); }
            }
            else if (service is IDisposable disposable)
            {
                try { disposable.Dispose(); }
                catch (Exception ex) { Logger.Warn(ex, "[DataAcquisitionService] Communication service dispose failed in DisposeAsync"); }
            }
        }
        _communicationServices.Clear();

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // 同步路径：不阻塞等待 StopAsync（避免 sync-over-async 死锁）
        try { _cts?.Cancel(); }
        catch (ObjectDisposedException) { }

        foreach (var service in _communicationServices.Values)
        {
            if (service is IDisposable disposable)
            {
                try { disposable.Dispose(); }
                catch (Exception ex) { Logger.Warn(ex, "[DataAcquisitionService] Communication service dispose failed in Dispose"); }
            }
        }
        _communicationServices.Clear();

        // 给后台任务有限的清理时间，不无限等待
        try { _runningTask?.Wait(TimeSpan.FromSeconds(2)); }
        catch (Exception ex) { Logger.Warn(ex, "[DataAcquisitionService] RunningTask wait timeout during Dispose"); }

        _cts?.Dispose();
        _runningTask?.Dispose();

        GC.SuppressFinalize(this);
    }
}
