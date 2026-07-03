using HVAC.EnergyMonitor.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel;

namespace HVAC.EnergyMonitor.Models.Entities;

public class Point : IDataErrorInfo
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

    public string Error => string.Empty;

    public string this[string columnName]
    {
        get
        {
            return columnName switch
            {
                nameof(Name) => string.IsNullOrWhiteSpace(Name) ? "点位名称不能为空" :
                                Name.Length > 100 ? "点位名称长度不能超过 100" : string.Empty,
                nameof(RegisterAddress) => RegisterAddress < 0 ? "寄存器地址不能为负数" : string.Empty,
                nameof(FunctionCode) => FunctionCode is not (1 or 2 or 3 or 4) ? "功能码仅支持 1/2/3/4" : string.Empty,
                nameof(Scale) => Scale == 0 ? "系数不能为 0" : string.Empty,
                nameof(DeviceId) => DeviceId <= 0 ? "必须关联有效设备" : string.Empty,
                _ => string.Empty
            };
        }
    }
}
