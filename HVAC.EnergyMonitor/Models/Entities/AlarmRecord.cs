using HVAC.EnergyMonitor.Models.Enums;
using System;

namespace HVAC.EnergyMonitor.Models.Entities;

public class AlarmRecord
{
    public int Id { get; set; }
    public int PointId { get; set; }
    public AlarmType AlarmType { get; set; }
    public double TriggerValue { get; set; }
    public double LimitValue { get; set; }
    public DateTime TriggerTime { get; set; } = DateTime.Now;
    public bool Acknowledged { get; set; } = false;
    public DateTime? AckTime { get; set; }

    public Point Point { get; set; } = null!;
}
