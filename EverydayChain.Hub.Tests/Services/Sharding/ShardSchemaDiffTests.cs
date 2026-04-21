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
/// 分表结构差异计算测试。
/// </summary>
public class ShardSchemaDiffTests
{
    /// <summary>业务任务逻辑表名。</summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>
    /// 结构完全一致时不应重复生成 DDL。
    /// </summary>
    [Fact]
    public void BuildDiff_ShouldBeIdempotent_WhenSchemaAlreadyAligned()
    {
        var synchronizer = CreateSynchronizer();
        var template = synchronizer.ResolveTableTemplate(BusinessTaskLogicalTable);
        var physicalSchema = new ShardPhysicalTableSchema(
            template.Schema,
            "business_tasks_202604",
            template.Columns,
            template.PrimaryKeyColumns,
            template.Indexes
                .Select(index => new ShardIndexSchema(
                    ShardSchemaTemplateBuilder.BuildPhysicalIndexName(template.LogicalTable, "business_tasks_202604", index.DatabaseName),
                    index.IsUnique,
                    index.ColumnNames))
                .ToList());

        var diff = synchronizer.BuildDiff(template, physicalSchema);
        var sql = synchronizer.BuildSynchronizationSql("business_tasks_202604", template, diff);

        Assert.False(diff.HasChanges);
        Assert.Empty(diff.MissingColumns);
        Assert.Empty(diff.MissingIndexes);
        Assert.Empty(sql);
    }

    /// <summary>
    /// 非空且无安全默认值的缺失列应只告警不执行自动补齐。
    /// </summary>
    [Fact]
    public void BuildDiff_ShouldWarnAndSkipUnsafeRequiredColumn()
    {
        var synchronizer = CreateSynchronizer();
        var template = synchronizer.ResolveTableTemplate(BusinessTaskLogicalTable);
        var customTemplate = template with
        {
            Columns = template.Columns
                .Concat([new ShardColumnSchema("RequiredColumn", "int", false, false, null, null, 999)])
                .ToList()
        };
        var physicalSchema = new ShardPhysicalTableSchema(
            customTemplate.Schema,
            "business_tasks_202604",
            template.Columns,
            template.PrimaryKeyColumns,
            template.Indexes
                .Select(index => new ShardIndexSchema(
                    ShardSchemaTemplateBuilder.BuildPhysicalIndexName(template.LogicalTable, "business_tasks_202604", index.DatabaseName),
                    index.IsUnique,
                    index.ColumnNames))
                .ToList());

        var diff = synchronizer.BuildDiff(customTemplate, physicalSchema);

        Assert.DoesNotContain(diff.MissingColumns, column => string.Equals(column.ColumnName, "RequiredColumn", StringComparison.Ordinal));
        Assert.Contains(diff.Warnings, warning => warning.Contains("RequiredColumn", StringComparison.Ordinal));
    }

    /// <summary>
    /// 创建分表结构同步器。
    /// </summary>
    /// <returns>同步器实例。</returns>
    private static ShardSchemaSynchronizer CreateSynchronizer()
    {
        return new ShardSchemaSynchronizer(
            Options.Create(new ShardingOptions
            {
                Schema = "dbo",
                ConnectionString = "Server=localhost;Database=EverydayChainHub_UnitTest;Trusted_Connection=True;TrustServerCertificate=True;"
            }),
            [BusinessTaskLogicalTable],
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
}
