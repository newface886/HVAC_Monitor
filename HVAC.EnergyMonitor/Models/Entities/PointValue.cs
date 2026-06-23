using HVAC.EnergyMonitor.Models.Enums;
using System;

namespace HVAC.EnergyMonitor.Models.Entities;

public class PointValue
{
    public long Id { get; set; }
    public int PointId { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public Quality Quality { get; set; } = Quality.Good;

    public Point Point { get; set; } = null!;
}
