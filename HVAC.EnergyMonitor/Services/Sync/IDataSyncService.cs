using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Sync;

public interface IDataSyncService
{
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
