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
/// 定义当前类型。
/// </summary>
public class ShardSchemaDiffTests
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string TestConnectionString = "Server=localhost;Database=EverydayChainHub_UnitTest;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public void BuildSynchronizationSql_ShouldReturnEmpty_WhenPhysicalTableAlreadyAligned()
    {
        var synchronizer = CreateSynchronizer();
        var template = synchronizer.ResolveTableTemplate(BusinessTaskLogicalTable);
        /// <summary>
        /// 存储当前字段值。
        /// </summary>
        const string physicalTableName = "business_tasks_202604";
        var physicalSchema = BuildAlignedPhysicalSchema(template, physicalTableName);

        var diff = synchronizer.BuildDiff(template, physicalSchema);
        var sql = synchronizer.BuildSynchronizationSql(physicalTableName, template, diff);

        Assert.False(diff.HasChanges);
        Assert.Empty(diff.MissingColumns);
        Assert.Empty(diff.MissingIndexes);
        Assert.Empty(sql);
        Assert.DoesNotContain("ALTER TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE INDEX", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildDiff_ShouldNotReportMissingIndex_WhenEquivalentIndexAlreadyExists()
    {
        var synchronizer = CreateSynchronizer();
        var template = synchronizer.ResolveTableTemplate(BusinessTaskLogicalTable);
        var equivalentIndex = Assert.Single(FindIndexesByColumn(template.Indexes, "WorkingArea"));

        var physicalSchema = new ShardPhysicalTableSchema(
            template.Schema,
            "business_tasks_202604",
            template.Columns,
            template.PrimaryKeyColumns,
            [
                new ShardIndexSchema("IX_business_tasks_202604_WorkingArea_CustomName", equivalentIndex.IsUnique, equivalentIndex.ColumnNames)
            ]);

        var diff = synchronizer.BuildDiff(template, physicalSchema);

        Assert.DoesNotContain(diff.MissingIndexes, index =>
            index.IsUnique == equivalentIndex.IsUnique
            && index.ColumnNames.SequenceEqual(equivalentIndex.ColumnNames, StringComparer.Ordinal));
    }

    [Fact]
    public void BuildDiff_ShouldWarnAndSkip_WhenMissingNonNullableColumnHasNoSafeDefaultValue()
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
        var sql = synchronizer.BuildSynchronizationSql("business_tasks_202604", customTemplate, diff);

        Assert.DoesNotContain(diff.MissingColumns, column => string.Equals(column.ColumnName, "RequiredColumn", StringComparison.Ordinal));
        Assert.Contains(diff.Warnings, warning => warning.Contains("RequiredColumn", StringComparison.Ordinal));
        Assert.DoesNotContain("ALTER TABLE [dbo].[business_tasks_202604] ADD [RequiredColumn] int NOT NULL", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static ShardPhysicalTableSchema BuildAlignedPhysicalSchema(ShardTableSchemaTemplate template, string physicalTableName)
    {
        return new ShardPhysicalTableSchema(
            template.Schema,
            physicalTableName,
            template.Columns,
            template.PrimaryKeyColumns,
            template.Indexes
                .Select(index => new ShardIndexSchema(
                    ShardSchemaTemplateBuilder.BuildPhysicalIndexName(template.LogicalTable, physicalTableName, index.DatabaseName),
                    index.IsUnique,
                    index.ColumnNames))
                .ToList());
    }

    private static IEnumerable<ShardIndexSchema> FindIndexesByColumn(IEnumerable<ShardIndexSchema> indexes, string columnName)
    {
        return indexes
            .Where(index =>
                !index.IsUnique
                && index.ColumnNames.Count == 1
                && string.Equals(index.ColumnNames[0], columnName, StringComparison.Ordinal));
    }

    private static ShardSchemaSynchronizer CreateSynchronizer()
    {
        return new ShardSchemaSynchronizer(
            Options.Create(new ShardingOptions
            {
                Schema = "dbo",
                ConnectionString = TestConnectionString
            }),
            [BusinessTaskLogicalTable],
            CreateDbContextFactory(),
            new StubShardTableResolver(),
            new PassThroughDangerZoneExecutor(),
            NullLogger<ShardSchemaSynchronizer>.Instance);
    }

    private static IDbContextFactory<HubDbContext> CreateDbContextFactory()
    {
        var contextOptions = new DbContextOptionsBuilder<HubDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;
        var shardingOptions = Options.Create(new ShardingOptions
        {
            Schema = "dbo"
        });

        return new HubDbContextTestFactory(contextOptions, shardingOptions);
    }
}

