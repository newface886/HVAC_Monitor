using HVAC.EnergyMonitor.Infrastructure.DbContext;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Infrastructure.Repository;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly AppDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        return (IRepository<T>)_repositories.GetOrAdd(type, _ => new Repository<T>(_context));
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
