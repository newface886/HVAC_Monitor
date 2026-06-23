using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Communication;

public class ModbusTcpCommunicationService : ICommunicationService
{
    private string _ipAddress = "127.0.0.1";
    private int _port = 502;
    private TcpClient? _tcpClient;
    private bool _connected;

    public string Name => "ModbusTCP";
    public bool IsConnected => _connected && _tcpClient?.Connected == true;

    public void Configure(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            _tcpClient?.Dispose();
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_ipAddress, _port);
            _connected = true;
            return true;
        }
        catch
        {
            _connected = false;
            return false;
        }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _connected = false;
        _tcpClient?.Close();
        _tcpClient?.Dispose();
        _tcpClient = null;
        return Task.CompletedTask;
    }

    public Task<ushort[]> ReadHoldingRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken ct = default)
    {
        throw new NotImplementedException("Real Modbus TCP integration pending. Use Simulator for now.");
    }

    public Task<ushort[]> ReadInputRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken ct = default)
    {
        throw new NotImplementedException("Real Modbus TCP integration pending. Use Simulator for now.");
    }
}
