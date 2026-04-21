using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Services.Sharding;
using EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;
using EverydayChain.Hub.Tests.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Services.Sharding;

/// <summary>
/// ShardSchemaSynchronizer 结构同步测试。
/// </summary>
public class ShardSchemaSynchronizerTests
{
    /// <summary>业务任务逻辑表名。</summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>落格日志逻辑表名。</summary>
    private const string DropLogLogicalTable = "drop_logs";

    /// <summary>
    /// EF 模型模板应正确提取 WorkingArea 列与相关索引。
    /// </summary>
    [Fact]
    public void ResolveTableTemplate_ShouldContainWorkingAreaColumnAndIndexes()
    {
        var synchronizer = CreateSynchronizer(BusinessTaskLogicalTable);

        var template = synchronizer.ResolveTableTemplate(BusinessTaskLogicalTable);

        Assert.Contains(template.Columns, column =>
            string.Equals(column.ColumnName, "WorkingArea", StringComparison.Ordinal)
            && string.Equals(column.StoreType, "nvarchar(32)", StringComparison.OrdinalIgnoreCase)
            && column.IsNullable);
        Assert.Contains(template.Indexes, index => string.Equals(index.DatabaseName, "IX_business_tasks_WorkingArea", StringComparison.Ordinal));
        Assert.Contains(template.Indexes, index => string.Equals(index.DatabaseName, "IX_business_tasks_NormalizedWaveCode_SourceType_WorkingArea", StringComparison.Ordinal));
    }

    /// <summary>
    /// 历史分表缺列时应生成 WorkingArea 补列 SQL。
    /// </summary>
    [Fact]
    public void BuildSynchronizationSql_ShouldGenerateAddColumnSql_WhenWorkingAreaMissing()
    {
        var synchronizer = CreateSynchronizer(BusinessTaskLogicalTable);
        var template = synchronizer.ResolveTableTemplate(BusinessTaskLogicalTable);
        var physicalSchema = new ShardPhysicalTableSchema(
            template.Schema,
            "business_tasks_202604",
            template.Columns.Where(column => !string.Equals(column.ColumnName, "WorkingArea", StringComparison.Ordinal)).ToList(),
            template.PrimaryKeyColumns,
            BuildPhysicalIndexes(template, "business_tasks_202604"));

        var diff = synchronizer.BuildDiff(template, physicalSchema);
        var sql = synchronizer.BuildSynchronizationSql("business_tasks_202604", template, diff);

        Assert.Contains(diff.MissingColumns, column => string.Equals(column.ColumnName, "WorkingArea", StringComparison.Ordinal));
        Assert.Contains("COL_LENGTH(N'[dbo].[business_tasks_202604]', N'WorkingArea') IS NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ALTER TABLE [dbo].[business_tasks_202604] ADD [WorkingArea] nvarchar(32) NULL;", sql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 历史分表缺索引时应生成索引补齐 SQL。
    /// </summary>
    [Fact]
    public void BuildSynchronizationSql_ShouldGenerateCreateIndexSql_WhenWorkingAreaIndexesMissing()
    {
        var synchronizer = CreateSynchronizer(BusinessTaskLogicalTable);
        var template = synchronizer.ResolveTableTemplate(BusinessTaskLogicalTable);
        var physicalSchema = new ShardPhysicalTableSchema(
            template.Schema,
            "business_tasks_202604",
            template.Columns,
            template.PrimaryKeyColumns,
            BuildPhysicalIndexes(template, "business_tasks_202604")
                .Where(index => !index.DatabaseName.Contains("WorkingArea", StringComparison.Ordinal))
                .ToList());

        var diff = synchronizer.BuildDiff(template, physicalSchema);
        var sql = synchronizer.BuildSynchronizationSql("business_tasks_202604", template, diff);

        Assert.Contains(diff.MissingIndexes, index => string.Equals(index.DatabaseName, "IX_business_tasks_WorkingArea", StringComparison.Ordinal));
        Assert.Contains("CREATE INDEX [IX_business_tasks_202604_WorkingArea] ON [dbo].[business_tasks_202604] ([WorkingArea]);", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE INDEX [IX_business_tasks_202604_NormalizedWaveCode_SourceType_WorkingArea] ON [dbo].[business_tasks_202604] ([NormalizedWaveCode], [SourceType], [WorkingArea]);", sql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 同步器应支持多个纳管逻辑表复用同一套模板解析流程。
    /// </summary>
    [Fact]
    public void ResolveTableTemplate_ShouldSupportMultipleManagedLogicalTables()
    {
        var synchronizer = CreateSynchronizer(BusinessTaskLogicalTable, DropLogLogicalTable);

        var businessTaskTemplate = synchronizer.ResolveTableTemplate(BusinessTaskLogicalTable);
        var dropLogTemplate = synchronizer.ResolveTableTemplate(DropLogLogicalTable);

        Assert.Equal(BusinessTaskLogicalTable, businessTaskTemplate.LogicalTable);
        Assert.Equal(DropLogLogicalTable, dropLogTemplate.LogicalTable);
        Assert.NotEmpty(businessTaskTemplate.Columns);
        Assert.NotEmpty(dropLogTemplate.Columns);
    }

    /// <summary>
    /// 创建分表结构同步器。
    /// </summary>
    /// <param name="managedLogicalTables">纳管逻辑表。</param>
    /// <returns>同步器实例。</returns>
    private static ShardSchemaSynchronizer CreateSynchronizer(params string[] managedLogicalTables)
    {
        return new ShardSchemaSynchronizer(
            Options.Create(new ShardingOptions
            {
                Schema = "dbo",
                ConnectionString = "Server=localhost;Database=EverydayChainHub_UnitTest;Trusted_Connection=True;TrustServerCertificate=True;"
            }),
            managedLogicalTables,
            CreateDbContextFactory(),
            new StubShardTableResolver(),
            new PassThroughDangerZoneExecutor(),
            NullLogger<ShardSchemaSynchronizer>.Instance);
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
    /// 基于逻辑索引模板构建物理分表索引集合。
    /// </summary>
    /// <param name="template">逻辑表模板。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <returns>物理索引集合。</returns>
    private static List<ShardIndexSchema> BuildPhysicalIndexes(ShardTableSchemaTemplate template, string physicalTableName)
    {
        return template.Indexes
            .Select(index => new ShardIndexSchema(
                ShardSchemaTemplateBuilder.BuildPhysicalIndexName(template.LogicalTable, physicalTableName, index.DatabaseName),
                index.IsUnique,
                index.ColumnNames))
            .ToList();
    }
}
