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
/// 定义 ShardSchemaSynchronizerTests 类型。
/// </summary>
public class ShardSchemaSynchronizerTests
{
    /// <summary>
    /// 存储 BusinessTaskLogicalTable 字段。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>
    /// 存储 DropLogLogicalTable 字段。
    /// </summary>
    private const string DropLogLogicalTable = "drop_logs";

    /// <summary>
    /// 存储 TestConnectionString 字段。
    /// </summary>
    private const string TestConnectionString = "Server=localhost;Database=EverydayChainHub_UnitTest;Trusted_Connection=True;TrustServerCertificate=True;";

    /// <summary>
    /// 缓存读取 Int32 元数据私有方法的反射入口。
    /// </summary>
    private static readonly MethodInfo ReadInt32ValueMethod = ResolvePrivateStaticMethod("ReadInt32Value");

    /// <summary>
    /// 缓存读取 Int16 元数据私有方法的反射入口。
    /// </summary>
    private static readonly MethodInfo ReadInt16ValueMethod = ResolvePrivateStaticMethod("ReadInt16Value");

    /// <summary>
    /// 缓存读取 Byte 元数据私有方法的反射入口。
    /// </summary>
    private static readonly MethodInfo ReadByteValueMethod = ResolvePrivateStaticMethod("ReadByteValue");

    /// <summary>
    /// 缓存读取 Boolean 元数据私有方法的反射入口。
    /// </summary>
    private static readonly MethodInfo ReadBooleanValueMethod = ResolvePrivateStaticMethod("ReadBooleanValue");

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

    [Fact]
    public void ReadInt32Value_ShouldSupportByteValue()
    {
        using var reader = CreateReader("ColumnId", typeof(byte), (byte)7);

        var actual = InvokeReadInt32Value(reader, 0);

        Assert.Equal(7, actual);
    }

    [Fact]
    public void ReadInt32Value_ShouldSupportInt16Value()
    {
        using var reader = CreateReader("ColumnId", typeof(short), (short)9);

        var actual = InvokeReadInt32Value(reader, 0);

        Assert.Equal(9, actual);
    }

    [Fact]
    public void ReadInt32Value_ShouldSupportInt32Value()
    {
        using var reader = CreateReader("ColumnId", typeof(int), 11);

        var actual = InvokeReadInt32Value(reader, 0);

        Assert.Equal(11, actual);
    }

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

    [Fact]
    public void ReadInt16Value_ShouldSupportByteValue()
    {
        using var reader = CreateReader("MaxLength", typeof(byte), (byte)12);

        var actual = InvokeReadInt16Value(reader, 0);

        Assert.Equal(12, actual);
    }

    [Fact]
    public void ReadByteValue_ShouldSupportInt32Value()
    {
        using var reader = CreateReader("NumericScale", typeof(int), 6);

        var actual = InvokeReadByteValue(reader, 0);

        Assert.Equal((byte)6, actual);
    }

    [Fact]
    public void ReadBooleanValue_ShouldSupportBooleanValue()
    {
        using var reader = CreateReader("IsNullable", typeof(bool), true);

        var actual = InvokeReadBooleanValue(reader, 0);

        Assert.True(actual);
    }

    [Fact]
    public void ReadBooleanValue_ShouldSupportNumericBooleanValue()
    {
        using var trueReader = CreateReader("IsIdentity", typeof(byte), (byte)1);
        using var falseReader = CreateReader("IsNullable", typeof(short), (short)0);

        var trueValue = InvokeReadBooleanValue(trueReader, 0);
        var falseValue = InvokeReadBooleanValue(falseReader, 0);

        Assert.True(trueValue);
        Assert.False(falseValue);
    }

    [Fact]
    public void ReadInt32Value_ShouldNotThrow_WhenKeyOrdinalUnderlyingTypeIsByte()
    {
        using var reader = CreateReader("KeyOrdinal", typeof(byte), (byte)1);

        var actual = 0;
        var exception = Record.Exception(() => actual = InvokeReadInt32Value(reader, 0));

        Assert.Null(exception);
        Assert.Equal(1, actual);
    }

    [Fact]
    public void ReadByteValue_ShouldThrowChineseException_WhenValueIsDBNull()
    {
        using var reader = CreateReader("NumericPrecision", typeof(object), DBNull.Value);

        var exception = Assert.Throws<TargetInvocationException>(() => InvokeReadByteValue(reader, 0));

        var actualException = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("读取分表结构元数据失败", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("字段 NumericPrecision", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("Byte", actualException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadBooleanValue_ShouldThrowChineseException_WhenNumericValueIsNotZeroOrOne()
    {
        using var reader = CreateReader("IsNullable", typeof(int), 2);

        var exception = Assert.Throws<TargetInvocationException>(() => InvokeReadBooleanValue(reader, 0));

        var actualException = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("读取分表结构元数据失败", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("字段 IsNullable", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("System.Int32", actualException.Message, StringComparison.Ordinal);
        Assert.Contains("Boolean", actualException.Message, StringComparison.Ordinal);
    }

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

    private static List<ShardIndexSchema> BuildPhysicalIndexes(ShardTableSchemaTemplate template, string physicalTableName)
    {
        return template.Indexes
            .Select(index => new ShardIndexSchema(
                ShardSchemaTemplateBuilder.BuildPhysicalIndexName(template.LogicalTable, physicalTableName, index.DatabaseName),
                index.IsUnique,
                index.ColumnNames))
            .ToList();
    }

    private static MethodInfo ResolvePrivateStaticMethod(string methodName)
    {
        var method = typeof(ShardSchemaSynchronizer).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        if (method is not null)
        {
            return method;
        }

        throw new InvalidOperationException(
            $"未能在类型“{typeof(ShardSchemaSynchronizer).FullName}”上找到名为“{methodName}”的私有静态方法。该失败通常表示方法名、可见性或签名已发生变更，请同步更新测试代码。");
    }

    private static int InvokeReadInt32Value(DbDataReader reader, int ordinal)
    {
        return (int)ReadInt32ValueMethod.Invoke(null, [reader, ordinal])!;
    }

    private static short InvokeReadInt16Value(DbDataReader reader, int ordinal)
    {
        return (short)ReadInt16ValueMethod.Invoke(null, [reader, ordinal])!;
    }

    private static byte InvokeReadByteValue(DbDataReader reader, int ordinal)
    {
        return (byte)ReadByteValueMethod.Invoke(null, [reader, ordinal])!;
    }

    private static bool InvokeReadBooleanValue(DbDataReader reader, int ordinal)
    {
        return (bool)ReadBooleanValueMethod.Invoke(null, [reader, ordinal])!;
    }

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

