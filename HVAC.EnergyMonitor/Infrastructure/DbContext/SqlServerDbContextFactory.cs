using Microsoft.EntityFrameworkCore;

namespace HVAC.EnergyMonitor.Infrastructure.DbContext;

public class SqlServerDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public SqlServerDbContextFactory(string connectionString)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(_options);
    }
}
