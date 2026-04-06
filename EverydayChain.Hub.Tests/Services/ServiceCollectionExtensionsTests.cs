using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.DependencyInjection;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// ServiceCollectionExtensions 逻辑表构建行为测试。
/// </summary>
public class ServiceCollectionExtensionsTests
{
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
    /// 无可用启用逻辑表时应抛出配置异常。
    /// </summary>
    [Fact]
    public void BuildManagedLogicalTables_WithEmptyEnabledTables_ShouldThrow()
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

        var action = () => ServiceCollectionExtensions.BuildManagedLogicalTables(options);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("TargetLogicalTable 为空", ex.Message);
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

        Assert.Single(tables);
        Assert.Contains("Table_A", tables, StringComparer.OrdinalIgnoreCase);
    }
}
