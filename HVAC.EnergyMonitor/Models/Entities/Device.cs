using HVAC.EnergyMonitor.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel;

namespace HVAC.EnergyMonitor.Models.Entities;

public class Device : IDataErrorInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProtocolType ProtocolType { get; set; } = ProtocolType.Simulator;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 502;
    public string SerialPortName { get; set; } = string.Empty;
    public int BaudRate { get; set; } = 9600;
    public byte SlaveAddress { get; set; } = 1;
    public int ScanIntervalMs { get; set; } = 1000;
    public bool IsEnabled { get; set; } = true;

    public ICollection<Point> Points { get; set; } = new List<Point>();

    public string Error => string.Empty;

    public string this[string columnName]
    {
        get
        {
            return columnName switch
            {
                nameof(Name) => string.IsNullOrWhiteSpace(Name) ? "设备名称不能为空" :
                                Name.Length > 100 ? "设备名称长度不能超过 100" : string.Empty,
                nameof(ScanIntervalMs) => ScanIntervalMs < 100 ? "扫描周期不能小于 100 ms" : string.Empty,
                nameof(Port) => Port < 1 || Port > 65535 ? "端口范围必须为 1~65535" : string.Empty,
                nameof(BaudRate) => BaudRate < 0 ? "波特率不能为负数" : string.Empty,
                _ => string.Empty
            };
        }
    }
}
