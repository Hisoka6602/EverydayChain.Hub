using System.Data;
using System.Data.Common;
using System.Reflection;
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

    /// <summary>测试连接字符串。</summary>
    private const string TestConnectionString = "Server=localhost;Database=EverydayChainHub_UnitTest;Trusted_Connection=True;TrustServerCertificate=True;";

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
    /// 元数据读取应支持 byte 到 int 的安全转换。
    /// </summary>
    [Fact]
    public void ReadInt32Value_ShouldSupportByteValue()
    {
        using var reader = CreateReader("ColumnId", typeof(byte), (byte)7);

        var actual = InvokeReadInt32Value(reader, 0);

        Assert.Equal(7, actual);
    }

    /// <summary>
    /// 元数据读取应支持 short 到 int 的安全转换。
    /// </summary>
    [Fact]
    public void ReadInt32Value_ShouldSupportInt16Value()
    {
        using var reader = CreateReader("ColumnId", typeof(short), (short)9);

        var actual = InvokeReadInt32Value(reader, 0);

        Assert.Equal(9, actual);
    }

    /// <summary>
    /// 元数据读取应支持 int 直接读取。
    /// </summary>
    [Fact]
    public void ReadInt32Value_ShouldSupportInt32Value()
    {
        using var reader = CreateReader("ColumnId", typeof(int), 11);

        var actual = InvokeReadInt32Value(reader, 0);

        Assert.Equal(11, actual);
    }

    /// <summary>
    /// 元数据读取遇到 DBNull 时应抛出中文异常。
    /// </summary>
    [Fact]
    public void ReadInt32Value_ShouldThrowChineseException_WhenValueIsDBNull()
    {
        using var reader = CreateReader("ColumnId", typeof(object), DBNull.Value);

        var exception = Assert.Throws<TargetInvocationException>(() => InvokeReadInt32Value(reader, 0));

        var actualException = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("读取分表结构元数据失败", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("序号 0", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("字段 ColumnId", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("DBNull", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("Int32", actualException.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 索引顺序字段底层为 byte 时不应再触发 Byte 到 Int32 转换异常。
    /// </summary>
    [Fact]
    public void ReadPhysicalTableSchemaAsync_ShouldNotThrow_WhenKeyOrdinalUnderlyingTypeIsByte()
    {
        using var reader = CreateReader("KeyOrdinal", typeof(byte), (byte)1);

        var exception = Record.Exception(() => InvokeReadInt32Value(reader, 0));

        Assert.Null(exception);
        Assert.Equal(1, InvokeReadInt32Value(reader, 0));
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
                ConnectionString = TestConnectionString
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
            .UseSqlServer(TestConnectionString)
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

    /// <summary>
    /// 调用私有 Int32 元数据读取方法。
    /// </summary>
    /// <param name="reader">数据读取器。</param>
    /// <param name="ordinal">字段序号。</param>
    /// <returns>转换结果。</returns>
    private static int InvokeReadInt32Value(DbDataReader reader, int ordinal)
    {
        return (int)typeof(ShardSchemaSynchronizer)
            .GetMethod("ReadInt32Value", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [reader, ordinal])!;
    }

    /// <summary>
    /// 创建单列单行读取器。
    /// </summary>
    /// <param name="columnName">列名。</param>
    /// <param name="columnType">列类型。</param>
    /// <param name="value">列值。</param>
    /// <returns>定位到首行的读取器。</returns>
    private static DbDataReader CreateReader(string columnName, Type columnType, object value)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add(columnName, columnType);
        var row = dataTable.NewRow();
        row[columnName] = value;
        dataTable.Rows.Add(row);
        var reader = dataTable.CreateDataReader();
        reader.Read();
        return reader;
    }
}
