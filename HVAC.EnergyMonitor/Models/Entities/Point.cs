using HVAC.EnergyMonitor.Models.Enums;
using System.Collections.Generic;

namespace HVAC.EnergyMonitor.Models.Entities;

public class Point
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FunctionCode { get; set; } = 3;
    public int RegisterAddress { get; set; }
    public DataType DataType { get; set; } = DataType.UShort;
    public ByteOrder ByteOrder { get; set; } = ByteOrder.BigEndian;
    public double Scale { get; set; } = 1.0;
    public double Offset { get; set; } = 0.0;
    public string Unit { get; set; } = string.Empty;
    public double? HighLimit { get; set; }
    public double? LowLimit { get; set; }
    public bool StoreHistory { get; set; } = true;
    public bool IsEnabled { get; set; } = true;

    public Device Device { get; set; } = null!;
    public ICollection<PointValue> Values { get; set; } = new List<PointValue>();
    public ICollection<AlarmRule> AlarmRules { get; set; } = new List<AlarmRule>();
}
