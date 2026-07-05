using HVAC.EnergyMonitor.Infrastructure.Repository;
using HVAC.EnergyMonitor.Models.Entities;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Storage;

public class DataStorageService : IDataStorageService, IDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConcurrentQueue<PointValue> _buffer = new();
    private readonly Timer _flushTimer;
    private const int MaxBatchSize = 100;

    public DataStorageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _flushTimer = new Timer(_ => _ = FlushAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public Task EnqueueAsync(PointValue value)
    {
        _buffer.Enqueue(value);
        if (_buffer.Count >= MaxBatchSize)
        {
            _ = FlushAsync();
        }
        return Task.CompletedTask;
    }

    public async Task FlushAsync()
    {
        var batch = new List<PointValue>();
        while (_buffer.TryDequeue(out var value) && batch.Count < MaxBatchSize)
        {
            batch.Add(value);
        }

        if (batch.Count == 0) return;

        try
        {
            await _unitOfWork.Repository<PointValue>().AddRangeAsync(batch);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[DataStorageService] Flush failed: {Message}", ex.Message);
        }
    }

    public void Dispose()
    {
        _flushTimer.Dispose();
        _ = FlushAsync();
        GC.SuppressFinalize(this);
    }
}
