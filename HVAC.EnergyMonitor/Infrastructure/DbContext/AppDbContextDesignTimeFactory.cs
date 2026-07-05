using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HVAC.EnergyMonitor.Infrastructure.DbContext;

/// <summary>
/// 设计时 DbContext 工厂，供 EF Core CLI 工具 (dotnet ef) 在不启动 WPF 宿主的情况下创建 DbContext。
/// 运行时由 AppDbContextFactory（DI 注入）接管。
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ProjectRoot = @"d:\study\623WPFstudy";
    private const string DbFileName = "hvac_energy_monitor.db";

    public AppDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(ProjectRoot, DbFileName);
        var connectionString = $"Data Source={dbPath}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
