using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 分表预置服务实现，在 SQL Server 中按需创建分表与索引。
/// </summary>
public class ShardTableProvisioner(
    IOptions<ShardingOptions> options,
    IReadOnlyList<string> managedLogicalTables,
    IDbContextFactory<HubDbContext> dbContextFactory,
    ILogger<ShardTableProvisioner> logger,
    IDangerZoneExecutor dangerZoneExecutor) : IShardTableProvisioner
{
    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = options.Value;
    /// <summary>纳管逻辑表列表。</summary>
    private readonly IReadOnlyList<string> _managedLogicalTables = ValidateManagedLogicalTables(managedLogicalTables);
    /// <summary>分表预建并发上限。</summary>
    private readonly int _preProvisionMaxConcurrency = NormalizePreProvisionConcurrency(options.Value.PreProvisionMaxConcurrency);
    /// <summary>按逻辑表缓存的实体结构模板。</summary>
    private readonly IReadOnlyDictionary<string, TableTemplate> _tableTemplates = BuildTableTemplates(dbContextFactory, managedLogicalTables);

    /// <inheritdoc/>
    public async Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(_preProvisionMaxConcurrency, _preProvisionMaxConcurrency);
        var tasks = new List<Task>();
        foreach (var suffix in suffixes)
        {
            foreach (var logicalTable in _managedLogicalTables)
            {
                var tableCode = logicalTable;
                var shardSuffix = suffix;
                tasks.Add(RunWithConcurrencyAsync(
                    semaphore,
                    token => EnsureShardTableAsync(tableCode, shardSuffix, token),
                    cancellationToken));
            }
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    /// <inheritdoc/>
    public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken)
    {
        var tasks = _managedLogicalTables.Select(logicalTable => EnsureShardTableAsync(logicalTable, suffix, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// 确保指定逻辑表与后缀对应的物理分表存在。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="suffix">分表后缀。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private Task EnsureShardTableAsync(string logicalTable, string suffix, CancellationToken cancellationToken) => dangerZoneExecutor.ExecuteAsync(
        $"ensure-shard-table-{logicalTable}-{suffix}",
        async token =>
        {
            var tableName = $"{logicalTable}{suffix}";
            var fullName = $"[{_options.Schema}].[{tableName}]";
            var template = ResolveTableTemplate(logicalTable);

            // 生成幂等建表 DDL：按逻辑表对应实体模型创建字段与索引。
            var sql = BuildCreateTableSql(template, tableName, fullName);

            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(token);
            logger.LogInformation("分表自治: 已确认分表存在 {Table}", fullName);
        },
        cancellationToken);

    /// <summary>
    /// 构建幂等建表 SQL。
    /// </summary>
    /// <param name="template">逻辑表模板。</param>
    /// <param name="tableName">目标表名。</param>
    /// <param name="fullName">Schema 全限定名。</param>
    /// <returns>建表 SQL。</returns>
    private string BuildCreateTableSql(TableTemplate template, string tableName, string fullName)
    {
        var columnDefinitions = string.Join(
            "," + Environment.NewLine,
            template.Columns.Select(BuildColumnDefinition));

        var primaryKeySql = template.PrimaryKeyColumns.Count == 0
            ? string.Empty
            : $",{Environment.NewLine}    CONSTRAINT [PK_{tableName}] PRIMARY KEY ({string.Join(", ", template.PrimaryKeyColumns.Select(column => $"[{column}]"))})";
        var indexSql = string.Join(
            Environment.NewLine,
            template.Indexes.Select(index => BuildIndexSql(index, tableName, fullName)));
        if (!string.IsNullOrWhiteSpace(indexSql))
        {
            indexSql = Environment.NewLine + indexSql;
        }

        return $@"
IF OBJECT_ID(N'{_options.Schema}.{tableName}', N'U') IS NULL
BEGIN
    CREATE TABLE {fullName}
    (
    {columnDefinitions}{primaryKeySql}
    );{indexSql}
END";
    }

    /// <summary>
    /// 校验并冻结纳管逻辑表集合，防止运行时注入非法标识符。
    /// </summary>
    /// <param name="managedLogicalTables">待校验逻辑表集合。</param>
    /// <returns>校验通过的只读逻辑表列表。</returns>
    /// <exception cref="InvalidOperationException">存在空值或非法标识符时抛出。</exception>
    private static IReadOnlyList<string> ValidateManagedLogicalTables(IReadOnlyList<string> managedLogicalTables)
    {
        ArgumentNullException.ThrowIfNull(managedLogicalTables);
        if (managedLogicalTables.Count == 0)
        {
            throw new InvalidOperationException("分表配置无效：纳管逻辑表集合为空，无法执行分表预建。");
        }

        foreach (var logicalTable in managedLogicalTables)
        {
            if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(logicalTable))
            {
                throw new InvalidOperationException($"分表配置无效：逻辑表名 '{logicalTable}' 非法，仅允许字母、数字、下划线。");
            }
        }

        return managedLogicalTables;
    }

    /// <summary>
    /// 基于 EF 实体模型构建逻辑表模板缓存。
    /// </summary>
    /// <param name="dbContextFactory">DbContext 工厂。</param>
    /// <param name="managedLogicalTables">纳管逻辑表集合。</param>
    /// <returns>逻辑表模板字典。</returns>
    private static IReadOnlyDictionary<string, TableTemplate> BuildTableTemplates(
        IDbContextFactory<HubDbContext> dbContextFactory,
        IReadOnlyList<string> managedLogicalTables)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        var result = new Dictionary<string, TableTemplate>(StringComparer.OrdinalIgnoreCase);

        using var _ = TableSuffixScope.Use(string.Empty);
        using var dbContext = dbContextFactory.CreateDbContext();
        var model = dbContext.Model;
        foreach (var logicalTable in managedLogicalTables)
        {
            var entityType = model
                .GetEntityTypes()
                .FirstOrDefault(entity => string.Equals(entity.GetTableName(), logicalTable, StringComparison.OrdinalIgnoreCase));
            if (entityType is null)
            {
                throw new InvalidOperationException($"分表配置无效：逻辑表名 '{logicalTable}' 未找到对应实体模型映射。");
            }

            var tableIdentifier = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
            var columns = entityType
                .GetProperties()
                .Select(property => new ColumnTemplate(
                    property.GetColumnName(tableIdentifier) ?? property.Name,
                    property.GetColumnType() ?? ResolveFallbackStoreType(property),
                    property.IsNullable,
                    IsIdentity(property)))
                .ToList();
            var primaryKeyColumns = entityType.FindPrimaryKey()?.Properties
                .Select(property => property.GetColumnName(tableIdentifier) ?? property.Name)
                .ToList() ?? [];
            var indexes = entityType
                .GetIndexes()
                .Select(index => new IndexTemplate(
                    index.GetDatabaseName() ?? $"IX_{entityType.GetTableName()}_{string.Join("_", index.Properties.Select(property => property.Name))}",
                    index.IsUnique,
                    index.Properties.Select(property => property.GetColumnName(tableIdentifier) ?? property.Name).ToList()))
                .Where(index => index.ColumnNames.Count > 0)
                .ToList();

            result[logicalTable] = new TableTemplate(columns, primaryKeyColumns, indexes);
        }

        return result;
    }

    /// <summary>
    /// 解析逻辑表对应的模板。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <returns>实体结构模板。</returns>
    private TableTemplate ResolveTableTemplate(string logicalTable)
    {
        if (_tableTemplates.TryGetValue(logicalTable, out var template))
        {
            return template;
        }

        throw new InvalidOperationException($"分表配置无效：逻辑表名 '{logicalTable}' 未配置实体模型模板。");
    }

    /// <summary>
    /// 判断属性是否为自增列。
    /// </summary>
    /// <param name="property">实体属性元数据。</param>
    /// <returns>若为自增列返回 true，否则返回 false。</returns>
    private static bool IsIdentity(IProperty property)
    {
        if (property.ValueGenerated != ValueGenerated.OnAdd)
        {
            return false;
        }

        var underlyingType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return underlyingType == typeof(int) || underlyingType == typeof(long);
    }

    /// <summary>
    /// 构建列定义 SQL 片段。
    /// </summary>
    /// <param name="column">列模板。</param>
    /// <returns>列定义 SQL。</returns>
    private static string BuildColumnDefinition(ColumnTemplate column)
    {
        var identitySql = column.IsIdentity ? " IDENTITY(1,1)" : string.Empty;
        var nullabilitySql = column.IsNullable ? " NULL" : " NOT NULL";
        return $"    [{column.ColumnName}] {column.StoreType}{identitySql}{nullabilitySql}";
    }

    /// <summary>
    /// 构建索引定义 SQL 片段。
    /// </summary>
    /// <param name="index">索引模板。</param>
    /// <param name="tableName">表名。</param>
    /// <param name="fullName">Schema 全限定表名。</param>
    /// <returns>索引定义 SQL。</returns>
    private static string BuildIndexSql(IndexTemplate index, string tableName, string fullName)
    {
        var uniqueSql = index.IsUnique ? "UNIQUE " : string.Empty;
        var indexName = index.DatabaseName.Replace("sorting_task_trace", tableName, StringComparison.OrdinalIgnoreCase);
        var columnsSql = string.Join(", ", index.ColumnNames.Select(column => $"[{column}]"));
        return $"    CREATE {uniqueSql}INDEX [{indexName}] ON {fullName}({columnsSql});";
    }

    /// <summary>
    /// 当实体属性未显式声明列类型时推断 SQL Server 存储类型。
    /// </summary>
    /// <param name="property">实体属性元数据。</param>
    /// <returns>SQL Server 存储类型。</returns>
    private static string ResolveFallbackStoreType(IProperty property)
    {
        var underlyingType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return underlyingType switch
        {
            var valueType when valueType == typeof(string) => "nvarchar(max)",
            var valueType when valueType == typeof(int) => "int",
            var valueType when valueType == typeof(long) => "bigint",
            var valueType when valueType == typeof(DateTime) => "datetime2",
            var valueType when valueType == typeof(DateTimeOffset) => "datetimeoffset",
            var valueType when valueType == typeof(decimal) => "decimal(18,8)",
            _ => throw new InvalidOperationException($"分表模板解析失败：属性 {property.DeclaringType.DisplayName()}.{property.Name} 缺少可识别的 SQL 类型映射。")
        };
    }

    /// <summary>
    /// 归一化并发上限配置值。
    /// </summary>
    /// <param name="configuredConcurrency">配置并发上限。</param>
    /// <returns>限制到 1-64 范围后的并发值。</returns>
    private static int NormalizePreProvisionConcurrency(int configuredConcurrency)
    {
        return Math.Clamp(configuredConcurrency, 1, 64);
    }

    /// <summary>
    /// 在信号量限制下执行异步任务。
    /// </summary>
    /// <param name="semaphore">并发控制信号量。</param>
    /// <param name="action">待执行动作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private static async Task RunWithConcurrencyAsync(
        SemaphoreSlim semaphore,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// 列模板。
    /// </summary>
    /// <param name="ColumnName">列名。</param>
    /// <param name="StoreType">数据库类型。</param>
    /// <param name="IsNullable">是否可空。</param>
    /// <param name="IsIdentity">是否自增。</param>
    private readonly record struct ColumnTemplate(string ColumnName, string StoreType, bool IsNullable, bool IsIdentity);

    /// <summary>
    /// 索引模板。
    /// </summary>
    /// <param name="DatabaseName">索引名。</param>
    /// <param name="IsUnique">是否唯一。</param>
    /// <param name="ColumnNames">索引列集合。</param>
    private readonly record struct IndexTemplate(string DatabaseName, bool IsUnique, IReadOnlyList<string> ColumnNames);

    /// <summary>
    /// 逻辑表模板。
    /// </summary>
    /// <param name="Columns">列集合。</param>
    /// <param name="PrimaryKeyColumns">主键列集合。</param>
    /// <param name="Indexes">索引集合。</param>
    private readonly record struct TableTemplate(
        IReadOnlyList<ColumnTemplate> Columns,
        IReadOnlyList<string> PrimaryKeyColumns,
        IReadOnlyList<IndexTemplate> Indexes);

}
