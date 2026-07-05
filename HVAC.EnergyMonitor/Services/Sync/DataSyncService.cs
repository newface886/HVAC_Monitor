using HVAC.EnergyMonitor.Infrastructure.DbContext;
using HVAC.EnergyMonitor.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Sync;

public class DataSyncService : IDataSyncService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private const string TablePointValues = "PointValues";
    private const string TableAlarmRecords = "AlarmRecords";

    private readonly IDbContextFactory<AppDbContext> _sqliteFactory;
    private readonly IDbContextFactory<AppDbContext> _sqlServerFactory;
    private readonly IConfiguration _configuration;

    private readonly bool _enabled;
    private readonly int _intervalSec;
    private readonly int _batchSize;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private int _consecutiveFailures;

    public DataSyncService(
        IDbContextFactory<AppDbContext> sqliteFactory,
        IDbContextFactory<AppDbContext> sqlServerFactory,
        IConfiguration configuration)
    {
        _sqliteFactory = sqliteFactory;
        _sqlServerFactory = sqlServerFactory;
        _configuration = configuration;

        var appSettings = configuration.GetSection("AppSettings");
        _enabled = appSettings.GetValue<bool>("SyncEnabled", true);
        _intervalSec = Math.Max(1, appSettings.GetValue<int>("SyncIntervalSec", 2));
        _batchSize = Math.Max(1, appSettings.GetValue<int>("SyncBatchSize", 500));
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        if (!_enabled)
        {
            Logger.Info("[DataSyncService] SyncEnabled=false, 同步未启动");
            return Task.CompletedTask;
        }
        if (_loopTask != null)
        {
            Logger.Warn("[DataSyncService] 已经在运行中，忽略重复 Start");
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
        Logger.Info("[DataSyncService] 已启动: interval={IntervalSec}s, batchSize={BatchSize}",
            _intervalSec, _batchSize);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_cts == null) return;

        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException) { return; }

        if (_loopTask != null)
        {
            try
            {
                await _loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* 正常取消 */ }
            catch (Exception ex)
            {
                Logger.Error(ex, "[DataSyncService] Stop caught exception: {Message}", ex.Message);
            }
        }

        _cts.Dispose();
        _cts = null;
        _loopTask = null;
        Logger.Info("[DataSyncService] 已停止");
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await TickAsync(ct).ConfigureAwait(false);
                _consecutiveFailures = 0;
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures == 1 || _consecutiveFailures % 30 == 0)
                {
                    Logger.Error(ex, "[DataSyncService] Tick 失败 (连续 {Count} 次): {Message}",
                        _consecutiveFailures, ex.Message);
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_intervalSec), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return; }
        }
    }

    public async Task TickAsync(CancellationToken ct = default)
    {
        var watermarks = await ReadAndInitWatermarksAsync(ct).ConfigureAwait(false);

        var newPointValues = await ReadNewPointValuesAsync(watermarks[TablePointValues], ct).ConfigureAwait(false);
        var newAlarmRecords = await ReadNewAlarmRecordsAsync(watermarks[TableAlarmRecords], ct).ConfigureAwait(false);

        if (newPointValues.Count == 0 && newAlarmRecords.Count == 0) return;

        using (var sqlCtx = _sqlServerFactory.CreateDbContext())
        {
            sqlCtx.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));
            if (newPointValues.Count > 0)
            {
                await sqlCtx.PointValues.AddRangeAsync(newPointValues, ct).ConfigureAwait(false);
            }
            if (newAlarmRecords.Count > 0)
            {
                await sqlCtx.AlarmRecords.AddRangeAsync(newAlarmRecords, ct).ConfigureAwait(false);
            }
            await sqlCtx.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var newPvId = newPointValues.Count > 0 ? newPointValues.Max(v => v.Id) : watermarks[TablePointValues];
        var newArId = newAlarmRecords.Count > 0 ? newAlarmRecords.Max(r => r.Id) : watermarks[TableAlarmRecords];
        await UpdateWatermarksAsync(newPvId, newArId, ct).ConfigureAwait(false);

        Logger.Info("[DataSyncService] 已同步: PointValues={PvCount}, AlarmRecords={ArCount}",
            newPointValues.Count, newAlarmRecords.Count);
    }

    private async Task<Dictionary<string, long>> ReadAndInitWatermarksAsync(CancellationToken ct)
    {
        var dict = new Dictionary<string, long>
        {
            [TablePointValues] = 0,
            [TableAlarmRecords] = 0
        };

        using var ctx = _sqliteFactory.CreateDbContext();
        var states = await ctx.SyncStates.AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

        var anyInserted = false;
        foreach (var (table, _) in dict.ToList())
        {
            var existing = states.FirstOrDefault(s => s.TableName == table);
            if (existing != null)
            {
                dict[table] = existing.LastSyncedRowId;
            }
            else
            {
                ctx.SyncStates.Add(new SyncState
                {
                    TableName = table,
                    LastSyncedRowId = 0,
                    LastSyncTime = DateTime.MinValue
                });
                anyInserted = true;
            }
        }

        if (anyInserted)
        {
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return dict;
    }

    private async Task<List<PointValue>> ReadNewPointValuesAsync(long afterId, CancellationToken ct)
    {
        using var ctx = _sqliteFactory.CreateDbContext();
        return await ctx.PointValues.AsNoTracking()
            .Where(v => v.Id > afterId)
            .OrderBy(v => v.Id)
            .Take(_batchSize)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    private async Task<List<AlarmRecord>> ReadNewAlarmRecordsAsync(long afterId, CancellationToken ct)
    {
        using var ctx = _sqliteFactory.CreateDbContext();
        return await ctx.AlarmRecords.AsNoTracking()
            .Where(r => r.Id > afterId)
            .OrderBy(r => r.Id)
            .Take(_batchSize)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    private async Task UpdateWatermarksAsync(long pointValuesId, long alarmRecordsId, CancellationToken ct)
    {
        using var ctx = _sqliteFactory.CreateDbContext();
        var states = await ctx.SyncStates.ToListAsync(ct).ConfigureAwait(false);
        var now = DateTime.Now;

        foreach (var state in states)
        {
            if (state.TableName == TablePointValues)
            {
                state.LastSyncedRowId = pointValuesId;
                state.LastSyncTime = now;
            }
            else if (state.TableName == TableAlarmRecords)
            {
                state.LastSyncedRowId = alarmRecordsId;
                state.LastSyncTime = now;
            }
        }

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
