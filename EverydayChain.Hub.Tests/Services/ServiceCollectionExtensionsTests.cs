using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// ServiceCollectionExtensions 逻辑表构建行为测试。
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// AddInfrastructure 应正确绑定并注入日志表保留期配置集合。
    /// </summary>
    [Fact]
    public void AddInfrastructure_WithRetentionLogTablesConfig_ShouldBindAndInject()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Sharding:ConnectionString"] = "Server=localhost;Database=EverydayChainHub_UnitTest;Trusted_Connection=True;TrustServerCertificate=True;",
            ["RetentionJob:LogTables:0:Enabled"] = "true",
            ["RetentionJob:LogTables:0:LogicalTableName"] = "scan_logs",
            ["RetentionJob:LogTables:0:KeepMonths"] = "3",
            ["RetentionJob:LogTables:0:DryRun"] = "true",
            ["RetentionJob:LogTables:0:AllowDrop"] = "false",
            ["RetentionJob:LogTables:1:Enabled"] = "true",
            ["RetentionJob:LogTables:1:LogicalTableName"] = "bad-name",
            ["RetentionJob:LogTables:1:KeepMonths"] = "6",
            ["RetentionJob:LogTables:1:DryRun"] = "true",
            ["RetentionJob:LogTables:1:AllowDrop"] = "false"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        var logTables = provider.GetRequiredService<IReadOnlyList<RetentionLogTableOptions>>();

        Assert.Equal(2, logTables.Count);
        Assert.Equal("scan_logs", logTables[0].LogicalTableName);
        Assert.Equal("bad-name", logTables[1].LogicalTableName);
        Assert.True(logTables[0].DryRun);
        Assert.False(logTables[0].AllowDrop);
    }

    /// <summary>
    /// 非法逻辑表名应抛出配置异常。
    /// </summary>
    [Fact]
    public void BuildManagedLogicalTables_WithInvalidIdentifier_ShouldThrow()
    {
        var options = new SyncJobOptions
        {
            Tables =
            [
                new SyncTableOptions
                {
                    TableCode = "T1",
                    Enabled = true,
                    TargetLogicalTable = "bad-name"
                }
            ]
        };

        var action = () => ServiceCollectionExtensions.BuildManagedLogicalTables(options);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("非法逻辑表名", ex.Message);
    }

    /// <summary>
    /// 无启用同步表时仍应包含固定纳管逻辑表。
    /// </summary>
    [Fact]
    public void BuildManagedLogicalTables_WithEmptyEnabledTables_ShouldContainFixedManagedTables()
    {
        var options = new SyncJobOptions
        {
            Tables =
            [
                new SyncTableOptions
                {
                    TableCode = "T1",
                    Enabled = false,
                    TargetLogicalTable = "sorting_task_trace"
                }
            ]
        };

        var tables = ServiceCollectionExtensions.BuildManagedLogicalTables(options);
        Assert.Equal(5, tables.Count);
        Assert.Contains("sorting_task_trace", tables);
        Assert.Contains("sync_batches", tables);
        Assert.Contains("business_tasks", tables);
        Assert.Contains("scan_logs", tables);
        Assert.Contains("drop_logs", tables);
    }

    /// <summary>
    /// 逻辑表名应按大小写不敏感规则去重。
    /// </summary>
    [Fact]
    public void BuildManagedLogicalTables_WithCaseInsensitiveDuplicates_ShouldDeduplicate()
    {
        var options = new SyncJobOptions
        {
            Tables =
            [
                new SyncTableOptions
                {
                    TableCode = "T1",
                    Enabled = true,
                    TargetLogicalTable = "Table_A"
                },
                new SyncTableOptions
                {
                    TableCode = "T2",
                    Enabled = true,
                    TargetLogicalTable = "table_a"
                },
                new SyncTableOptions
                {
                    TableCode = "T3",
                    Enabled = true,
                    TargetLogicalTable = "TABLE_A"
                }
            ]
        };

        var tables = ServiceCollectionExtensions.BuildManagedLogicalTables(options);

        Assert.Equal(6, tables.Count);
        Assert.Contains("sorting_task_trace", tables);
        Assert.Contains("sync_batches", tables);
        Assert.Contains("business_tasks", tables);
        Assert.Contains("scan_logs", tables);
        Assert.Contains("drop_logs", tables);
        Assert.Contains("Table_A", tables);
    }

    /// <summary>
    /// 启用表的逻辑表名为空白时应立即抛出配置异常。
    /// </summary>
    [Fact]
    public void BuildManagedLogicalTables_WithEnabledTableAndBlankTargetLogicalTable_ShouldThrow()
    {
        var options = new SyncJobOptions
        {
            Tables =
            [
                new SyncTableOptions
                {
                    TableCode = "T1",
                    Enabled = true,
                    TargetLogicalTable = "   "
                }
            ]
        };

        var action = () => ServiceCollectionExtensions.BuildManagedLogicalTables(options);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("T1", ex.Message);
        Assert.Contains("不能为空白", ex.Message);
    }
}
