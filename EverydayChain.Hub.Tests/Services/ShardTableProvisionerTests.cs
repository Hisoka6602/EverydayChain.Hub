using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// ShardTableProvisioner 行为测试。
/// </summary>
public class ShardTableProvisionerTests
{
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
            ["sorting_task_trace"],
            CreateDbContextFactory(),
            NullLogger<ShardTableProvisioner>.Instance,
            new PassThroughDangerZoneExecutor());

        await provisioner.EnsureShardTablesAsync([], CancellationToken.None);
    }

    /// <summary>
    /// 分拣追踪表模板应保留字符串长度与索引定义。
    /// </summary>
    [Fact]
    public void BuildCreateTableSql_ForSortingTaskTrace_ShouldContainBoundedStringColumnsAndIndexes()
    {
        var provisioner = CreateProvisioner("sorting_task_trace");
        var template = GetTableTemplate(provisioner, "sorting_task_trace");

        var sql = BuildCreateTableSql(
            provisioner,
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
    public void BuildCreateTableSql_ForWmsPickToWcs_ShouldContainPrimaryKeyAndDecimalColumns()
    {
        var provisioner = CreateProvisioner("IDX_PICKTOWCS2");
        var template = GetTableTemplate(provisioner, "IDX_PICKTOWCS2");

        var sql = BuildCreateTableSql(
            provisioner,
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
    /// 获取指定逻辑表对应模板对象。
    /// </summary>
    /// <param name="provisioner">分表预置服务实例。</param>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <returns>模板对象。</returns>
    private static object GetTableTemplate(ShardTableProvisioner provisioner, string logicalTable)
    {
        var tableTemplatesField = typeof(ShardTableProvisioner).GetField("_tableTemplates", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("测试反射失败：未找到 _tableTemplates 字段。");
        var tableTemplates = tableTemplatesField.GetValue(provisioner) as System.Collections.IDictionary
            ?? throw new InvalidOperationException("测试反射失败：_tableTemplates 字段值为空。");
        var template = tableTemplates[logicalTable];
        return template ?? throw new InvalidOperationException($"测试反射失败：逻辑表 {logicalTable} 未找到模板。");
    }

    /// <summary>
    /// 调用私有建表 SQL 生成方法。
    /// </summary>
    /// <param name="provisioner">分表预置服务实例。</param>
    /// <param name="template">模板对象。</param>
    /// <param name="tableName">目标表名。</param>
    /// <param name="fullName">全限定表名。</param>
    /// <returns>建表 SQL。</returns>
    private static string BuildCreateTableSql(ShardTableProvisioner provisioner, object template, string tableName, string fullName)
    {
        var buildSqlMethod = typeof(ShardTableProvisioner).GetMethod("BuildCreateTableSql", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("测试反射失败：未找到 BuildCreateTableSql 方法。");
        var sql = buildSqlMethod.Invoke(provisioner, [template, tableName, fullName]) as string;
        return sql ?? throw new InvalidOperationException("测试反射失败：BuildCreateTableSql 返回空字符串。");
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
