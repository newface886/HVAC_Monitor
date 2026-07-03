using HVAC.EnergyMonitor.Models.Enums;
using System;

namespace HVAC.EnergyMonitor.Models;

public class PointDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Quality Quality { get; set; }
}
