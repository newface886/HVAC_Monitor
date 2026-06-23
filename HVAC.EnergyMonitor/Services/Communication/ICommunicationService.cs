using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Communication;

public interface ICommunicationService
{
    string Name { get; }
    bool IsConnected { get; }
    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<ushort[]> ReadHoldingRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken ct = default);
    Task<ushort[]> ReadInputRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken ct = default);
}
