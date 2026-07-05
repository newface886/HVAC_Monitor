using HVAC.EnergyMonitor.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Sync;

public class SqlServerSchemaManager : ISqlServerSchemaManager
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private static readonly string[] ExpectedTables =
        { "Devices", "Points", "PointValues", "AlarmRules", "AlarmRecords" };

    private readonly IDbContextFactory<AppDbContext> _sqlServerFactory;

    public SqlServerSchemaManager(IDbContextFactory<AppDbContext> sqlServerFactory)
    {
        _sqlServerFactory = sqlServerFactory;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        try
        {
            using var ctx = _sqlServerFactory.CreateDbContext();
            ctx.Database.SetCommandTimeout(TimeSpan.FromSeconds(10));

            if (!await ctx.Database.CanConnectAsync(ct).ConfigureAwait(false))
            {
                Logger.Warn("[SqlServerSchemaManager] SQL Server 不可达，跳过 schema 校验（同步将自动降级）");
                return;
            }

            var created = await ctx.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
            if (created)
            {
                Logger.Info("[SqlServerSchemaManager] SQL Server 数据库/表结构首次创建完成");
            }
            else
            {
                Logger.Info("[SqlServerSchemaManager] SQL Server 数据库已存在");
            }

            var existing = await GetExistingTablesAsync(ctx, ct).ConfigureAwait(false);
            var missing = ExpectedTables.Where(t => !existing.Contains(t)).ToList();

            if (missing.Count == 0)
            {
                Logger.Info("[SqlServerSchemaManager] Schema 验证通过: {Count} 张业务表全部存在", ExpectedTables.Length);
            }
            else
            {
                Logger.Error("[SqlServerSchemaManager] Schema 验证失败，缺失表: {Missing}",
                    string.Join(", ", missing));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[SqlServerSchemaManager] EnsureSchema failed: {Message}", ex.Message);
        }
    }

    private static async Task<HashSet<string>> GetExistingTablesAsync(
        AppDbContext ctx, CancellationToken ct)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conn = ctx.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct).ConfigureAwait(false);
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'";
        using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }
}
