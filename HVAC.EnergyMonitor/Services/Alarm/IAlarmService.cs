using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Models.Enums;
using HVAC.EnergyMonitor.Services.Cache;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Alarm;

public interface IAlarmService
{
    event EventHandler<AlarmEventArgs>? AlarmTriggered;
    Task CheckAsync(PointValueCacheItem value);
    Task<IEnumerable<AlarmRecord>> GetActiveAlarmsAsync();
    Task AcknowledgeAsync(int alarmRecordId);
}

public class AlarmEventArgs : EventArgs
{
    public AlarmRecord Record { get; }
    public AlarmEventArgs(AlarmRecord record) => Record = record;
}
