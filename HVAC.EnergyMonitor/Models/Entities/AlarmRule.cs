namespace HVAC.EnergyMonitor.Models.Entities;

public class AlarmRule
{
    public int Id { get; set; }
    public int PointId { get; set; }
    public double? HighLimit { get; set; }
    public double? LowLimit { get; set; }
    public bool IsEnabled { get; set; } = true;

    public Point Point { get; set; } = null!;
}
