using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Models.Enums;
using NLog;

namespace HVAC.EnergyMonitor.Services.Communication;

public class CommunicationServiceFactory : ICommunicationServiceFactory
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public ICommunicationService Create(ProtocolType protocolType, Device device)
    {
        return protocolType switch
        {
            ProtocolType.Simulator => new SimulatorCommunicationService(),
            ProtocolType.ModbusTCP => CreateModbusTcpService(device),
            ProtocolType.ModbusRTU => CreateModbusRtuService(device),
            _ => new SimulatorCommunicationService()
        };
    }

    private static ICommunicationService CreateModbusTcpService(Device device)
    {
        var service = new ModbusTcpCommunicationService();
        service.Configure(device.IpAddress, device.Port);
        return service;
    }

    private static ICommunicationService CreateModbusRtuService(Device device)
    {
        Logger.Warn("[CommunicationServiceFactory] ModbusRTU 尚未实现，设备 {DeviceName} 将使用 Simulator 兜底", device.Name);
        return new SimulatorCommunicationService();
    }
}
