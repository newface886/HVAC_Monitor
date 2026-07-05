using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Sync;

public interface ISqlServerSchemaManager
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
}
