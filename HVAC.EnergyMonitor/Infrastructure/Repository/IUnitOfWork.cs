using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Infrastructure.Repository;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
}
