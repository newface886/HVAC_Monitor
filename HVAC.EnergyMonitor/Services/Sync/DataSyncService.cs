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

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (!_enabled)
        {
            Logger.Info("[DataSyncService] SyncEnabled=false, 同步未启动");
            return;
        }
        if (_loopTask != null)
        {
            Logger.Warn("[DataSyncService] 已经在运行中，忽略重复 Start");
            return;
        }

        // 启动前先做一次参考数据全量同步（Devices/Points/AlarmRules）
        // 不然 PointValues 写入会因 FK 约束失败
        try
        {
            await SyncReferenceDataAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[DataSyncService] 参考数据同步失败，继续启动增量同步: {Message}", ex.Message);
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
        Logger.Info("[DataSyncService] 已启动: interval={IntervalSec}s, batchSize={BatchSize}",
            _intervalSec, _batchSize);
    }

    private async Task SyncReferenceDataAsync(CancellationToken ct)
    {
        // 读取 SQLite 端所有参考数据
        List<Device> devices;
        List<Point> points;
        List<AlarmRule> alarmRules;
        using (var sqliteCtx = _sqliteFactory.CreateDbContext())
        {
            devices = await sqliteCtx.Devices.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
            points = await sqliteCtx.Points.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
            alarmRules = await sqliteCtx.AlarmRules.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        }

        if (devices.Count == 0 && points.Count == 0 && alarmRules.Count == 0)
        {
            Logger.Info("[DataSyncService] SQLite 无参考数据，跳过参考数据同步");
            return;
        }

        using var sqlCtx = _sqlServerFactory.CreateDbContext();
        sqlCtx.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));
        using var tx = await sqlCtx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            // 临时禁用 FK 约束，避免删除/插入顺序问题
            await sqlCtx.Database.ExecuteSqlRawAsync(
                "ALTER TABLE dbo.AlarmRules NOCHECK CONSTRAINT ALL", ct).ConfigureAwait(false);
            await sqlCtx.Database.ExecuteSqlRawAsync(
                "ALTER TABLE dbo.AlarmRecords NOCHECK CONSTRAINT ALL", ct).ConfigureAwait(false);
            await sqlCtx.Database.ExecuteSqlRawAsync(
                "ALTER TABLE dbo.PointValues NOCHECK CONSTRAINT ALL", ct).ConfigureAwait(false);

            // 清空参考表（按依赖反序：AlarmRules → Points → Devices）
            await sqlCtx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.AlarmRules", ct).ConfigureAwait(false);
            await sqlCtx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.Points", ct).ConfigureAwait(false);
            await sqlCtx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.Devices", ct).ConfigureAwait(false);

            // 重新插入（按依赖正序：Devices → Points → AlarmRules），保留 SQLite RowId
            if (devices.Count > 0)
            {
                await sqlCtx.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT dbo.Devices ON", ct).ConfigureAwait(false);
                await sqlCtx.Devices.AddRangeAsync(devices, ct).ConfigureAwait(false);
                await sqlCtx.SaveChangesAsync(ct).ConfigureAwait(false);
                await sqlCtx.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT dbo.Devices OFF", ct).ConfigureAwait(false);
            }
            if (points.Count > 0)
            {
                await sqlCtx.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT dbo.Points ON", ct).ConfigureAwait(false);
                await sqlCtx.Points.AddRangeAsync(points, ct).ConfigureAwait(false);
                await sqlCtx.SaveChangesAsync(ct).ConfigureAwait(false);
                await sqlCtx.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT dbo.Points OFF", ct).ConfigureAwait(false);
            }
            if (alarmRules.Count > 0)
            {
                await sqlCtx.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT dbo.AlarmRules ON", ct).ConfigureAwait(false);
                await sqlCtx.AlarmRules.AddRangeAsync(alarmRules, ct).ConfigureAwait(false);
                await sqlCtx.SaveChangesAsync(ct).ConfigureAwait(false);
                await sqlCtx.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT dbo.AlarmRules OFF", ct).ConfigureAwait(false);
            }

            // 恢复 FK 约束
            await sqlCtx.Database.ExecuteSqlRawAsync(
                "ALTER TABLE dbo.PointValues WITH CHECK CHECK CONSTRAINT ALL", ct).ConfigureAwait(false);
            await sqlCtx.Database.ExecuteSqlRawAsync(
                "ALTER TABLE dbo.AlarmRecords WITH CHECK CHECK CONSTRAINT ALL", ct).ConfigureAwait(false);
            await sqlCtx.Database.ExecuteSqlRawAsync(
                "ALTER TABLE dbo.AlarmRules WITH CHECK CHECK CONSTRAINT ALL", ct).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }

        Logger.Info("[DataSyncService] 参考数据同步完成: Devices={Dc}, Points={Pc}, AlarmRules={Ac}",
            devices.Count, points.Count, alarmRules.Count);
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
            // 事务包裹 IDENTITY_INSERT + INSERT，保证 SET 在 SaveChanges 同一连接上生效
            using var tx = await sqlCtx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                if (newPointValues.Count > 0)
                {
                    // 同步保留 SQLite 端 RowId，必须用 IDENTITY_INSERT 显式赋值
                    await sqlCtx.Database.ExecuteSqlRawAsync(
                        "SET IDENTITY_INSERT dbo.PointValues ON", ct).ConfigureAwait(false);
                    await sqlCtx.PointValues.AddRangeAsync(newPointValues, ct).ConfigureAwait(false);
                    await sqlCtx.SaveChangesAsync(ct).ConfigureAwait(false);
                    await sqlCtx.Database.ExecuteSqlRawAsync(
                        "SET IDENTITY_INSERT dbo.PointValues OFF", ct).ConfigureAwait(false);
                }
                if (newAlarmRecords.Count > 0)
                {
                    await sqlCtx.Database.ExecuteSqlRawAsync(
                        "SET IDENTITY_INSERT dbo.AlarmRecords ON", ct).ConfigureAwait(false);
                    await sqlCtx.AlarmRecords.AddRangeAsync(newAlarmRecords, ct).ConfigureAwait(false);
                    await sqlCtx.SaveChangesAsync(ct).ConfigureAwait(false);
                    await sqlCtx.Database.ExecuteSqlRawAsync(
                        "SET IDENTITY_INSERT dbo.AlarmRecords OFF", ct).ConfigureAwait(false);
                }
                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
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
