using HVAC.EnergyMonitor.Models.Entities;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Storage;

public interface IDataStorageService
{
    Task EnqueueAsync(PointValue value);
    Task FlushAsync();
}
