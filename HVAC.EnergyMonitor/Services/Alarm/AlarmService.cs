using HVAC.EnergyMonitor.Infrastructure.DbContext;
using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Models.Enums;
using HVAC.EnergyMonitor.Services.Cache;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Alarm;

public class AlarmService : IAlarmService
{
    public event EventHandler<AlarmEventArgs>? AlarmTriggered;

    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ConcurrentDictionary<string, byte> _activeAlarms = new();

    public AlarmService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task CheckAsync(PointValueCacheItem value, CancellationToken ct = default)
    {
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var point = await context.Points
                .AsNoTracking()
                .Include(p => p.AlarmRules)
                .FirstOrDefaultAsync(p => p.Id == value.PointId, ct).ConfigureAwait(false);

            if (point == null) return;

            foreach (var rule in point.AlarmRules.Where(r => r.IsEnabled))
                {
                    if (rule.HighLimit.HasValue && value.Value > rule.HighLimit.Value)
                    {
                        await TriggerAlarmAsync(value, AlarmType.High, rule.HighLimit.Value, ct).ConfigureAwait(false);
                    }
                    else if (rule.LowLimit.HasValue && value.Value < rule.LowLimit.Value)
                    {
                        await TriggerAlarmAsync(value, AlarmType.Low, rule.LowLimit.Value, ct).ConfigureAwait(false);
                    }
                }

                if (point.HighLimit.HasValue && value.Value > point.HighLimit.Value)
                {
                    await TriggerAlarmAsync(value, AlarmType.High, point.HighLimit.Value, ct).ConfigureAwait(false);
                }
                if (point.LowLimit.HasValue && value.Value < point.LowLimit.Value)
                {
                    await TriggerAlarmAsync(value, AlarmType.Low, point.LowLimit.Value, ct).ConfigureAwait(false);
                }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[AlarmService] CheckAsync failed: {Message}", ex.Message);
        }
    }

    private async Task TriggerAlarmAsync(PointValueCacheItem value, AlarmType type, double limit, CancellationToken ct)
    {
        var key = $"{value.PointId}-{type}";
        if (_activeAlarms.ContainsKey(key)) return;

        _activeAlarms.TryAdd(key, 0);
        var record = new AlarmRecord
        {
            PointId = value.PointId,
            AlarmType = type,
            TriggerValue = value.Value,
            LimitValue = limit,
            TriggerTime = DateTime.Now
        };

        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            await context.AlarmRecords.AddAsync(record, ct).ConfigureAwait(false);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            AlarmTriggered?.Invoke(this, new AlarmEventArgs(record));
            Logger.Info("Alarm triggered: PointId={PointId}, Type={AlarmType}, Value={Value}, Limit={Limit}",
                record.PointId, record.AlarmType, record.TriggerValue, record.LimitValue);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[AlarmService] TriggerAlarmAsync failed: {Message}", ex.Message);
        }
    }

    public async Task<IEnumerable<AlarmRecord>> GetActiveAlarmsAsync(CancellationToken ct = default)
    {
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.AlarmRecords
                .AsNoTracking()
                .Where(r => !r.Acknowledged)
                .OrderByDescending(r => r.TriggerTime)
                .ToListAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[AlarmService] GetActiveAlarmsAsync failed: {Message}", ex.Message);
            return Array.Empty<AlarmRecord>();
        }
    }

    public async Task AcknowledgeAsync(int alarmRecordId, CancellationToken ct = default)
    {
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var record = await context.AlarmRecords.FindAsync(new object[] { alarmRecordId }, ct).ConfigureAwait(false);
            if (record == null) return;

            record.Acknowledged = true;
            record.AckTime = DateTime.Now;
            context.AlarmRecords.Update(record);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            var key = $"{record.PointId}-{record.AlarmType}";
            _activeAlarms.TryRemove(key, out _);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[AlarmService] AcknowledgeAsync failed: {Message}", ex.Message);
        }
    }
}
