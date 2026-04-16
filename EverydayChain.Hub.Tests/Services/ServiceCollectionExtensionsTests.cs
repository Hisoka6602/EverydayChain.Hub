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
        Assert.Equal(2, tables.Count);
        Assert.Contains("sorting_task_trace", tables);
        Assert.Contains("sync_batches", tables);
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

        Assert.Equal(3, tables.Count);
        Assert.Contains("sorting_task_trace", tables);
        Assert.Contains("sync_batches", tables);
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
