using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// ShardTableProvisioner 行为测试。
/// </summary>
public class ShardTableProvisionerTests
{
    /// <summary>分拣追踪逻辑表名。</summary>
    private const string SortingTaskTraceLogicalTable = "sorting_task_trace";
    /// <summary>业务任务逻辑表名。</summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>
    /// 纳管逻辑表集合为空时应立即抛出异常。
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyManagedLogicalTables_ShouldThrow()
    {
        var options = Options.Create(new ShardingOptions());
        var action = () => _ = new ShardTableProvisioner(
            options,
            Array.Empty<string>(),
            CreateDbContextFactory(),
            NullLogger<ShardTableProvisioner>.Instance,
            new PassThroughDangerZoneExecutor());

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("纳管逻辑表集合为空", ex.Message);
    }

    /// <summary>
    /// 并发上限配置超出范围时应钳制后仍可执行。
    /// </summary>
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
            new PassThroughDangerZoneExecutor());

        await provisioner.EnsureShardTablesAsync([], CancellationToken.None);
    }

    /// <summary>
    /// 分拣追踪表模板应保留字符串长度与索引定义。
    /// </summary>
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

    /// <summary>
    /// 业务任务模板应保留幂等唯一索引与关键查询索引。
    /// </summary>
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

    /// <summary>
    /// 创建测试用 DbContext 工厂。
    /// </summary>
    /// <returns>HubDbContext 工厂实例。</returns>
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

    /// <summary>
    /// 创建指定逻辑表集合的分表预置服务实例。
    /// </summary>
    /// <param name="managedLogicalTables">纳管逻辑表列表。</param>
    /// <returns>分表预置服务实例。</returns>
    private static ShardTableProvisioner CreateProvisioner(params string[] managedLogicalTables)
    {
        return new ShardTableProvisioner(
            Options.Create(new ShardingOptions()),
            managedLogicalTables,
            CreateDbContextFactory(),
            NullLogger<ShardTableProvisioner>.Instance,
            new PassThroughDangerZoneExecutor());
    }
}
