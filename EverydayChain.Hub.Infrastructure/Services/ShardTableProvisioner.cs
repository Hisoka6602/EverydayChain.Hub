using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services.Sharding;
using EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public class ShardTableProvisioner(
    IOptions<ShardingOptions> options,
    IReadOnlyList<string> managedLogicalTables,
    IDbContextFactory<HubDbContext> dbContextFactory,
    ILogger<ShardTableProvisioner> logger,
    IDangerZoneExecutor dangerZoneExecutor) : IShardTableProvisioner
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int ProvisionCommandTimeoutSeconds = 30;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ShardingOptions _options = options.Value;

    private readonly IReadOnlyList<string> _managedLogicalTables = ShardSchemaTemplateBuilder.ValidateManagedLogicalTables(managedLogicalTables);

    private readonly int _preProvisionMaxConcurrency = NormalizePreProvisionConcurrency(options.Value.PreProvisionMaxConcurrency);

    private readonly IReadOnlyDictionary<string, ShardTableSchemaTemplate> _tableTemplates = ShardSchemaTemplateBuilder.BuildTableTemplates(dbContextFactory, managedLogicalTables);

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

    public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken)
    {
        var tasks = _managedLogicalTables.Select(logicalTable => EnsureShardTableAsync(logicalTable, suffix, cancellationToken));
        return Task.WhenAll(tasks);
    }

    public Task EnsureShardTableAsync(string logicalTable, string suffix, CancellationToken cancellationToken) => dangerZoneExecutor.ExecuteAsync(
        $"ensure-shard-table-{logicalTable}-{suffix}",
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        async token =>
        {
            var tableName = $"{logicalTable}{suffix}";
            var fullName = $"[{_options.Schema}].[{tableName}]";
            var template = ResolveTableTemplate(logicalTable);

            var sql = BuildCreateTableSql(template, tableName, fullName);

            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = ProvisionCommandTimeoutSeconds;
            await command.ExecuteNonQueryAsync(token);
            logger.LogInformation("分表预建: 已确认新分表存在 {Table}。该服务仅负责首次建表，不负责历史分表结构升级。", fullName);
        },
        cancellationToken);

    internal string BuildCreateTableSql(ShardTableSchemaTemplate template, string tableName, string fullName)
    {
        var columnDefinitions = string.Join(
            "," + Environment.NewLine,
            template.Columns.Select(BuildColumnDefinition));

        var primaryKeySql = BuildPrimaryKeySql(template.PrimaryKeyColumns, tableName);
        var indexSql = string.Join(
            Environment.NewLine,
            template.Indexes.Select(index => BuildIndexSql(index, template.LogicalTable, tableName, fullName)));
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

    internal ShardTableSchemaTemplate ResolveTableTemplate(string logicalTable)
    {
        if (_tableTemplates.TryGetValue(logicalTable, out var template))
        {
            return template;
        }

        throw new InvalidOperationException($"分表配置无效：逻辑表名 '{logicalTable}' 未配置实体模型模板。");
    }

    private static string BuildColumnDefinition(ShardColumnSchema column)
    {
        var identitySql = column.IsIdentity ? " IDENTITY(1,1)" : string.Empty;
        var nullabilitySql = column.IsNullable ? " NULL" : " NOT NULL";
        return $"    [{column.ColumnName}] {column.StoreType}{identitySql}{nullabilitySql}";
    }

    private static string BuildPrimaryKeySql(IReadOnlyList<string> primaryKeyColumns, string tableName)
    {
        if (primaryKeyColumns.Count == 0)
        {
            return string.Empty;
        }

        if (primaryKeyColumns.Count == 1
            && string.Equals(primaryKeyColumns[0], "Id", StringComparison.OrdinalIgnoreCase))
        {
            return $",{Environment.NewLine}    CONSTRAINT [PK_{tableName}] PRIMARY KEY CLUSTERED ([{primaryKeyColumns[0]}] DESC)";
        }

        return $",{Environment.NewLine}    CONSTRAINT [PK_{tableName}] PRIMARY KEY ({string.Join(", ", primaryKeyColumns.Select(column => $"[{column}]"))})";
    }

    private static string BuildIndexSql(ShardIndexSchema index, string logicalTable, string tableName, string fullName)
    {
        var uniqueSql = index.IsUnique ? "UNIQUE " : string.Empty;
        var indexName = ShardSchemaTemplateBuilder.BuildPhysicalIndexName(logicalTable, tableName, index.DatabaseName);
        var columnsSql = string.Join(", ", index.ColumnNames.Select(column => $"[{column}]"));
        return $"    CREATE {uniqueSql}INDEX [{indexName}] ON {fullName}({columnsSql});";
    }

    private static int NormalizePreProvisionConcurrency(int configuredConcurrency)
    {
        return Math.Clamp(configuredConcurrency, 1, 64);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static async Task RunWithConcurrencyAsync(
        SemaphoreSlim semaphore,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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
}

