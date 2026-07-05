using System;

namespace HVAC.EnergyMonitor.Models.Entities;

public class SyncState
{
    public int Id { get; set; }
    public string TableName { get; set; } = "";
    public long LastSyncedRowId { get; set; }
    public DateTime LastSyncTime { get; set; } = DateTime.MinValue;
}
