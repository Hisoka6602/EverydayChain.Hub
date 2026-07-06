using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public class ShardTableProvisionerTests
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SortingTaskTraceLogicalTable = "sorting_task_trace";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    [Fact]
    public void Constructor_WithEmptyManagedLogicalTables_ShouldThrow()
    {
        var options = Options.Create(new ShardingOptions());
        var action = () => _ = new ShardTableProvisioner(
            options,
            Array.Empty<string>(),
            CreateDbContextFactory(),
            NullLogger<ShardTableProvisioner>.Instance,
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new PassThroughDangerZoneExecutor());

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("纳管逻辑表集合为空", ex.Message);
    }

    [Fact]
    public async Task EnsureShardTablesAsync_WithOutOfRangeConcurrency_ShouldComplete()
    {
        var options = Options.Create(new ShardingOptions
        {
            PreProvisionMaxConcurrency = 0
        });

        var provisioner = new ShardTableProvisioner(
            options,
            [SortingTaskTraceLogicalTable],
            CreateDbContextFactory(),
            NullLogger<ShardTableProvisioner>.Instance,
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new PassThroughDangerZoneExecutor());

        await provisioner.EnsureShardTablesAsync([], CancellationToken.None);
    }

    [Fact]
    public void SortingTaskTraceTemplate_ShouldContainBoundedStringColumnsAndIndexes()
    {
        var provisioner = CreateProvisioner(SortingTaskTraceLogicalTable);
        var template = provisioner.ResolveTableTemplate(SortingTaskTraceLogicalTable);

        var sql = provisioner.BuildCreateTableSql(
            template,
            "sorting_task_trace_202604",
            "[dbo].[sorting_task_trace_202604]");

        Assert.Contains("[BusinessNo] nvarchar(32) NOT NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Channel] nvarchar(32) NOT NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE INDEX [IX_sorting_task_trace_202604_BusinessNo] ON [dbo].[sorting_task_trace_202604]([BusinessNo]);", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE INDEX [IX_sorting_task_trace_202604_CreatedAt] ON [dbo].[sorting_task_trace_202604]([CreatedAt]);", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BusinessTaskTemplate_ShouldContainProjectionUniqueIndexAndStatusIndex()
    {
        var provisioner = CreateProvisioner(BusinessTaskLogicalTable);
        var template = provisioner.ResolveTableTemplate(BusinessTaskLogicalTable);

        var sql = provisioner.BuildCreateTableSql(
            template,
            "business_tasks_202604",
            "[dbo].[business_tasks_202604]");

        Assert.Contains("[Id] bigint IDENTITY(1,1) NOT NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PRIMARY KEY CLUSTERED ([Id] DESC)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[SourceTableCode] nvarchar(64) NOT NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[BusinessKey] nvarchar(256) NOT NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE UNIQUE INDEX [IX_business_tasks_202604_SourceTableCode_BusinessKey] ON [dbo].[business_tasks_202604]([SourceTableCode], [BusinessKey]);", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE INDEX [IX_business_tasks_202604_Status] ON [dbo].[business_tasks_202604]([Status]);", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static IDbContextFactory<HubDbContext> CreateDbContextFactory()
    {
        var contextOptions = new DbContextOptionsBuilder<HubDbContext>()
            .UseSqlServer("Server=localhost;Database=EverydayChainHub_UnitTest;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        var shardingOptions = Options.Create(new ShardingOptions
        {
            Schema = "dbo"
        });

        return new HubDbContextTestFactory(contextOptions, shardingOptions);
    }

    private static ShardTableProvisioner CreateProvisioner(params string[] managedLogicalTables)
    {
        return new ShardTableProvisioner(
            Options.Create(new ShardingOptions()),
            managedLogicalTables,
            CreateDbContextFactory(),
            NullLogger<ShardTableProvisioner>.Instance,
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new PassThroughDangerZoneExecutor());
    }
}

