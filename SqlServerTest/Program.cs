using Microsoft.Data.SqlClient;

Console.WriteLine("==========================================");
Console.WriteLine("   SQL Server 连接测试工具");
Console.WriteLine("==========================================\n");

// 从环境变量读取连接串，避免在源码中硬编码凭据
// 使用方式：
//   $env:HVACM_SQL_CONNECTION="Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;Encrypt=True;"
//   dotnet run
var connectionString = Environment.GetEnvironmentVariable("HVACM_SQL_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("错误：未找到环境变量 HVACM_SQL_CONNECTION");
    Console.WriteLine("请先设置连接串，例如：");
    Console.WriteLine("  $env:HVACM_SQL_CONNECTION=\"Server=YOUR_SERVER;Database=hvacm_data;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True;\"");
    PauseAndExit();
    return;
}

const string TargetDb = "hvacm_data";

void PrintError(Exception ex)
{
    Console.WriteLine($"    失败：{ex.Message}");
    var inner = ex.InnerException;
    var depth = 0;
    while (inner is not null && depth < 3)
    {
        Console.WriteLine($"    内部：{inner.Message}");
        inner = inner.InnerException;
        depth++;
    }
}

// ===== Step 1: 验证连接并确保数据库存在 =====
Console.WriteLine($"[1/4] 连接 master 验证账号...");
try
{
    var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
    using var conn = new SqlConnection(builder.ConnectionString);
    conn.Open();

    using var verCmd = new SqlCommand("SELECT @@VERSION", conn);
    var version = (string)verCmd.ExecuteScalar()!;
    Console.WriteLine($"    连接成功");
    Console.WriteLine($"    版本：{version.Split('\n')[0]}");

    using var dbCmd = new SqlCommand($"SELECT DB_ID('{TargetDb}')", conn);
    var dbId = dbCmd.ExecuteScalar();
    if (dbId is null or DBNull)
    {
        Console.WriteLine($"    数据库 '{TargetDb}' 不存在，正在创建...");
        using var create = new SqlCommand($"CREATE DATABASE [{TargetDb}]", conn);
        create.ExecuteNonQuery();
        Console.WriteLine($"    创建成功");
    }
    else
    {
        Console.WriteLine($"    数据库 '{TargetDb}' 已存在");
    }
}
catch (Exception ex)
{
    PrintError(ex);
    Console.WriteLine("\n可能原因：");
    Console.WriteLine("  - 连接串中的账号密码错误");
    Console.WriteLine("  - SQL Server 服务未启动");
    Console.WriteLine("  - SQL Server Browser 服务未启动（命名实例必需）");
    Console.WriteLine("  - 实例名拼写与 SSMS 不一致");
    PauseAndExit();
    return;
}

// ===== Step 2: 连接业务库 =====
Console.WriteLine($"\n[2/4] 连接业务库 '{TargetDb}'...");
try
{
    var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = TargetDb };
    using var conn = new SqlConnection(builder.ConnectionString);
    conn.Open();
    Console.WriteLine("    连接成功");
}
catch (Exception ex)
{
    PrintError(ex);
    PauseAndExit();
    return;
}

// ===== Step 3: 建表 + 写入 + 查询 + 清理 =====
Console.WriteLine("\n[3/4] 建临时表 + 插入 + 查询 + 清理...");
try
{
    var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = TargetDb };
    using var conn = new SqlConnection(builder.ConnectionString);
    conn.Open();

    using (var ddl = new SqlCommand(@"
        IF OBJECT_ID('_TestConn', 'U') IS NOT NULL DROP TABLE _TestConn;
        CREATE TABLE _TestConn (
            Id         INT,
            Tag        NVARCHAR(50),
            InsertedAt DATETIME DEFAULT GETDATE()
        );", conn))
    {
        ddl.ExecuteNonQuery();
    }
    Console.WriteLine("    建表成功");

    using (var ins = new SqlCommand(
        "INSERT INTO _TestConn (Id, Tag) VALUES (@id, @tag)", conn))
    {
        ins.Parameters.AddWithValue("@id", 1);
        ins.Parameters.AddWithValue("@tag", "hello sql server");
        ins.ExecuteNonQuery();
    }
    Console.WriteLine("    插入成功");

    using (var q = new SqlCommand("SELECT COUNT(*) FROM _TestConn", conn))
    {
        var count = (int)q.ExecuteScalar()!;
        Console.WriteLine($"    查询成功，表内 {count} 条");
    }

    using (var drop = new SqlCommand("DROP TABLE _TestConn", conn))
    {
        drop.ExecuteNonQuery();
    }
    Console.WriteLine("    清理成功");
}
catch (Exception ex)
{
    PrintError(ex);
    PauseAndExit();
    return;
}

// ===== Step 4: 输出最终连接串（脱敏） =====
Console.WriteLine("\n[4/4] 全部测试通过！");
Console.WriteLine("\n可用于 appsettings.json 的连接串（请替换密码）：");
var masked = new SqlConnectionStringBuilder(connectionString)
{
    Password = "***"
};
Console.WriteLine($"  {masked.ConnectionString}");

PauseAndExit();

void PauseAndExit()
{
    Console.WriteLine("\n按任意键退出...");
    Console.ReadKey();
}
