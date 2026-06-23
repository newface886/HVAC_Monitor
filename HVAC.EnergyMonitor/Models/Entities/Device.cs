using HVAC.EnergyMonitor.Models.Enums;
using System.Collections.Generic;

namespace HVAC.EnergyMonitor.Models.Entities;

public class Device
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
}
