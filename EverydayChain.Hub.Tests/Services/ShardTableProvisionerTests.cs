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
    /// <summary>WMS 下发 WCS 逻辑表名。</summary>
    private const string WmsPickToWcsLogicalTable = "IDX_PICKTOWCS2";

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
    /// WMS 表模板应保留主键与高精度小数列类型。
    /// </summary>
    [Fact]
    public void WmsPickToWcsTemplate_ShouldContainPrimaryKeyAndDecimalColumns()
    {
        var provisioner = CreateProvisioner(WmsPickToWcsLogicalTable);
        var template = provisioner.ResolveTableTemplate(WmsPickToWcsLogicalTable);

        var sql = provisioner.BuildCreateTableSql(
            template,
            "IDX_PICKTOWCS2_202604",
            "[dbo].[IDX_PICKTOWCS2_202604]");

        Assert.Contains("[R_SYSID] nvarchar(30) NOT NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PRIMARY KEY ([R_SYSID])", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[LENGTH] decimal(18,8) NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[WIDTH] decimal(18,8) NULL", sql, StringComparison.OrdinalIgnoreCase);
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

        return new TestHubDbContextFactory(contextOptions, shardingOptions);
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

    /// <summary>
    /// HubDbContext 测试工厂。
    /// </summary>
    private sealed class TestHubDbContextFactory(
        DbContextOptions<HubDbContext> contextOptions,
        IOptions<ShardingOptions> shardingOptions) : IDbContextFactory<HubDbContext>
    {
        /// <inheritdoc/>
        public HubDbContext CreateDbContext()
        {
            return new HubDbContext(contextOptions, shardingOptions);
        }
    }
}
