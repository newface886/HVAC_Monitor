using System;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Communication;

public class SimulatorCommunicationService : ICommunicationService
{
    private readonly Random _random = new();
    private readonly DateTime _startTime = DateTime.Now;
    private bool _connected;

    public string Name => "Simulator";
    public bool IsConnected => _connected;

    public Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        _connected = true;
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _connected = false;
        return Task.CompletedTask;
    }

    public Task<ushort[]> ReadHoldingRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken ct = default)
    {
        return GenerateRegistersAsync(startAddress, count, ct);
    }

    public Task<ushort[]> ReadInputRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken ct = default)
    {
        return GenerateRegistersAsync(startAddress, count, ct);
    }

    private Task<ushort[]> GenerateRegistersAsync(int startAddress, int count, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            double t = (DateTime.Now - _startTime).TotalSeconds;
            double value = 1000 + 200 * Math.Sin(t * 0.1 + startAddress + i) + _random.Next(-50, 51);
            result[i] = (ushort)Math.Clamp(value, 0, 65535);
        }
        return Task.FromResult(result);
    }
}
