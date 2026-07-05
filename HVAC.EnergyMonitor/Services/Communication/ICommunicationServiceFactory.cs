using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Models.Enums;

namespace HVAC.EnergyMonitor.Services.Communication;

public interface ICommunicationServiceFactory
{
    ICommunicationService Create(ProtocolType protocolType, Device device);
}
