using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace EverydayChain.Hub.Tests.Architecture;

/// <summary>
/// 业务主表单一来源架构防回退测试。
/// </summary>
public class BusinessTaskSingleSourceArchitectureTests
{
    /// <summary>
    /// DbContext 模型中不应再包含本地镜像表映射。
    /// </summary>
    [Fact]
    public void HubDbContextModel_ShouldNotContainLocalIdxMirrorTables()
    {
        using var context = CreateDbContext();
        var tableNames = context.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(tableName => !string.IsNullOrWhiteSpace(tableName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("IDX_PICKTOLIGHT_CARTON1", tableNames);
        Assert.DoesNotContain("IDX_PICKTOWCS2", tableNames);
        Assert.Contains("business_tasks", tableNames);
    }

    /// <summary>
    /// 模型快照中不应再包含本地镜像表映射文本。
    /// </summary>
    [Fact]
    public void HubDbContextModelSnapshot_ShouldNotContainLocalIdxMirrorTableMappings()
    {
        var repositoryRoot = ResolveRepositoryRootPath();
        var snapshotPath = Path.Combine(
            repositoryRoot,
            "EverydayChain.Hub.Infrastructure",
            "Migrations",
            "HubDbContextModelSnapshot.cs");
        var snapshotText = File.ReadAllText(snapshotPath);

        Assert.DoesNotContain("IDX_PICKTOLIGHT_CARTON1", snapshotText, StringComparison.Ordinal);
        Assert.DoesNotContain("IDX_PICKTOWCS2", snapshotText, StringComparison.Ordinal);
    }

    /// <summary>
    /// WMS 两条状态驱动任务应固定投影到业务逻辑表，并保持分表读取不回退固定表。
    /// </summary>
    [Fact]
    public void HostAppSettings_WmsStatusDrivenTables_ShouldProjectToBusinessTasks()
    {
        var repositoryRoot = ResolveRepositoryRootPath();
        var hostProjectPath = Path.Combine(repositoryRoot, "EverydayChain.Hub.Host");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(hostProjectPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var syncTables = configuration.GetSection("SyncJob:Tables").Get<List<SyncTableOptions>>();
        Assert.NotNull(syncTables);

        var splitTable = syncTables!.Single(table => string.Equals(table.TableCode, "WmsSplitPickToLightCarton", StringComparison.Ordinal));
        var fullCaseTable = syncTables.Single(table => string.Equals(table.TableCode, "WmsPickToWcs", StringComparison.Ordinal));
        var shardingOptions = configuration.GetSection("Sharding").Get<ShardingOptions>();

        Assert.NotNull(shardingOptions);
        Assert.False(shardingOptions!.EnableLegacyBaseTableReadFallback);
        // 此处断言为逻辑表名，运行态实际写入目标为 business_tasks_yyyyMM 月分表。
        Assert.Equal("business_tasks", splitTable.TargetLogicalTable);
        Assert.Equal("business_tasks", fullCaseTable.TargetLogicalTable);
        Assert.Equal("StatusDriven", splitTable.SyncMode);
        Assert.Equal("StatusDriven", fullCaseTable.SyncMode);
        Assert.Equal("Split", splitTable.SourceType);
        Assert.Equal("FullCase", fullCaseTable.SourceType);
        Assert.False(string.IsNullOrWhiteSpace(splitTable.BusinessKeyColumn));
        Assert.False(string.IsNullOrWhiteSpace(fullCaseTable.BusinessKeyColumn));
    }

    /// <summary>
    /// 创建测试用数据库上下文。
    /// </summary>
    /// <returns>数据库上下文实例。</returns>
    private static HubDbContext CreateDbContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("HUB_TEST_SQLSERVER_CONNECTION_STRING")
            ?? "Server=127.0.0.1;Database=Placeholder;User Id=Placeholder;Password=Placeholder;TrustServerCertificate=True;";
        var dbContextOptions = new DbContextOptionsBuilder<HubDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var shardingOptions = Options.Create(new ShardingOptions
        {
            Schema = "dbo"
        });
        return new HubDbContext(dbContextOptions, shardingOptions);
    }

    /// <summary>
    /// 解析仓库根目录绝对路径。
    /// </summary>
    /// <returns>仓库根目录。</returns>
    private static string ResolveRepositoryRootPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "EverydayChain.Hub.sln");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("未找到仓库根目录，无法定位 appsettings.json 与模型快照文件。");
    }
}
