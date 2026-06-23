using HVAC.EnergyMonitor.Infrastructure.Repository;
using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Models.Enums;
using HVAC.EnergyMonitor.Services.Cache;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Alarm;

public class AlarmService : IAlarmService
{
    public event EventHandler<AlarmEventArgs>? AlarmTriggered;

    private readonly IUnitOfWork _unitOfWork;
    private readonly HashSet<string> _activeAlarms = new();

    public AlarmService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CheckAsync(PointValueCacheItem value)
    {
        var point = await _unitOfWork.Repository<Point>().GetByIdAsync(value.PointId);
        if (point == null) return;

        foreach (var rule in point.AlarmRules.Where(r => r.IsEnabled))
        {
            if (rule.HighLimit.HasValue && value.Value > rule.HighLimit.Value)
            {
                await TriggerAlarmAsync(value, AlarmType.High, rule.HighLimit.Value);
            }
            else if (rule.LowLimit.HasValue && value.Value < rule.LowLimit.Value)
            {
                await TriggerAlarmAsync(value, AlarmType.Low, rule.LowLimit.Value);
            }
        }

        if (point.HighLimit.HasValue && value.Value > point.HighLimit.Value)
        {
            await TriggerAlarmAsync(value, AlarmType.High, point.HighLimit.Value);
        }
        if (point.LowLimit.HasValue && value.Value < point.LowLimit.Value)
        {
            await TriggerAlarmAsync(value, AlarmType.Low, point.LowLimit.Value);
        }
    }

    private async Task TriggerAlarmAsync(PointValueCacheItem value, AlarmType type, double limit)
    {
        var key = $"{value.PointId}-{type}";
        if (_activeAlarms.Contains(key)) return;

        _activeAlarms.Add(key);
        var record = new AlarmRecord
        {
            PointId = value.PointId,
            AlarmType = type,
            TriggerValue = value.Value,
            LimitValue = limit,
            TriggerTime = DateTime.Now
        };

        await _unitOfWork.Repository<AlarmRecord>().AddAsync(record);
        await _unitOfWork.SaveChangesAsync();
        AlarmTriggered?.Invoke(this, new AlarmEventArgs(record));
    }

    public async Task<IEnumerable<AlarmRecord>> GetActiveAlarmsAsync()
    {
        return await _unitOfWork.Repository<AlarmRecord>()
            .FindAsync(r => !r.Acknowledged);
    }

    public async Task AcknowledgeAsync(int alarmRecordId)
    {
        var record = await _unitOfWork.Repository<AlarmRecord>().GetByIdAsync(alarmRecordId);
        if (record == null) return;

        record.Acknowledged = true;
        record.AckTime = DateTime.Now;
        _unitOfWork.Repository<AlarmRecord>().Update(record);
        await _unitOfWork.SaveChangesAsync();

        var key = $"{record.PointId}-{record.AlarmType}";
        _activeAlarms.Remove(key);
    }
}
