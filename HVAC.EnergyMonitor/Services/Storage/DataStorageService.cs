using HVAC.EnergyMonitor.Infrastructure.DbContext;
using HVAC.EnergyMonitor.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Storage;

public class DataStorageService : IDataStorageService, IAsyncDisposable, IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConcurrentQueue<PointValue> _buffer = new();
    private readonly Timer _flushTimer;
    private const int MaxBatchSize = 100;
    private bool _disposed;

    public DataStorageService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _flushTimer = new Timer(_ => _ = FlushAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public Task EnqueueAsync(PointValue value)
    {
        if (_disposed) return Task.CompletedTask;

        _buffer.Enqueue(value);
        if (_buffer.Count >= MaxBatchSize)
        {
            _ = FlushAsync();
        }
        return Task.CompletedTask;
    }

    public async Task FlushAsync()
    {
        if (_disposed) return;

        var batch = new List<PointValue>();
        while (_buffer.TryDequeue(out var value) && batch.Count < MaxBatchSize)
        {
            batch.Add(value);
        }

        if (batch.Count == 0) return;

        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            await context.PointValues.AddRangeAsync(batch).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[DataStorageService] Flush failed: {Message}", ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _flushTimer.Dispose();
        try
        {
            await FlushAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[DataStorageService] DisposeAsync flush failed: {Message}", ex.Message);
        }
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _flushTimer.Dispose();
        // 同步路径：给刷盘有限时间，不无限阻塞（避免 sync-over-async 死锁）
        try
        {
            if (!FlushAsync().Wait(TimeSpan.FromSeconds(3)))
            {
                Logger.Warn("[DataStorageService] Flush timeout on synchronous Dispose, buffered data may be lost");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[DataStorageService] Dispose flush failed: {Message}", ex.Message);
        }
        GC.SuppressFinalize(this);
    }
}
