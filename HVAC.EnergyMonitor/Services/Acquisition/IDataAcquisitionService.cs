using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Acquisition;

public interface IDataAcquisitionService
{
    bool IsRunning { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
