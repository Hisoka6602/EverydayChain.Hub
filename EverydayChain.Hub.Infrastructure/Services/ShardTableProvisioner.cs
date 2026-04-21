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
/// 分表预置服务实现，在 SQL Server 中按需首次创建新分表与索引。
/// </summary>
public class ShardTableProvisioner(
    IOptions<ShardingOptions> options,
    IReadOnlyList<string> managedLogicalTables,
    IDbContextFactory<HubDbContext> dbContextFactory,
    ILogger<ShardTableProvisioner> logger,
    IDangerZoneExecutor dangerZoneExecutor) : IShardTableProvisioner
{
    /// <summary>分表预建 DDL 命令超时秒数（危险动作隔离器）。</summary>
    private const int ProvisionCommandTimeoutSeconds = 30;

    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = options.Value;

    /// <summary>纳管逻辑表列表。</summary>
    private readonly IReadOnlyList<string> _managedLogicalTables = ShardSchemaTemplateBuilder.ValidateManagedLogicalTables(managedLogicalTables);

    /// <summary>分表预建并发上限。</summary>
    private readonly int _preProvisionMaxConcurrency = NormalizePreProvisionConcurrency(options.Value.PreProvisionMaxConcurrency);

    /// <summary>按逻辑表缓存的实体结构模板。</summary>
    private readonly IReadOnlyDictionary<string, ShardTableSchemaTemplate> _tableTemplates = ShardSchemaTemplateBuilder.BuildTableTemplates(dbContextFactory, managedLogicalTables);

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
    public Task EnsureShardTableAsync(string logicalTable, string suffix, CancellationToken cancellationToken) => dangerZoneExecutor.ExecuteAsync(
        $"ensure-shard-table-{logicalTable}-{suffix}",
        async token =>
        {
            var tableName = $"{logicalTable}{suffix}";
            var fullName = $"[{_options.Schema}].[{tableName}]";
            var template = ResolveTableTemplate(logicalTable);

            // 步骤1：基于当前 EF 模型快照生成建表 SQL。
            var sql = BuildCreateTableSql(template, tableName, fullName);

            // 步骤2：执行幂等建表 DDL，仅负责首次建表，不处理历史分表升级。
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = ProvisionCommandTimeoutSeconds;
            await command.ExecuteNonQueryAsync(token);
            logger.LogInformation("分表预建: 已确认新分表存在 {Table}。该服务仅负责首次建表，不负责历史分表结构升级。", fullName);
        },
        cancellationToken);

    /// <summary>
    /// 构建幂等建表 SQL。
    /// </summary>
    /// <param name="template">逻辑表模板。</param>
    /// <param name="tableName">目标表名。</param>
    /// <param name="fullName">Schema 全限定名。</param>
    /// <returns>建表 SQL。</returns>
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

    /// <summary>
    /// 解析逻辑表对应的模板。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <returns>实体结构模板。</returns>
    internal ShardTableSchemaTemplate ResolveTableTemplate(string logicalTable)
    {
        if (_tableTemplates.TryGetValue(logicalTable, out var template))
        {
            return template;
        }

        throw new InvalidOperationException($"分表配置无效：逻辑表名 '{logicalTable}' 未配置实体模型模板。");
    }

    /// <summary>
    /// 构建列定义 SQL 片段。
    /// </summary>
    /// <param name="column">列模板。</param>
    /// <returns>列定义 SQL。</returns>
    private static string BuildColumnDefinition(ShardColumnSchema column)
    {
        var identitySql = column.IsIdentity ? " IDENTITY(1,1)" : string.Empty;
        var nullabilitySql = column.IsNullable ? " NULL" : " NOT NULL";
        return $"    [{column.ColumnName}] {column.StoreType}{identitySql}{nullabilitySql}";
    }

    /// <summary>
    /// 构建主键定义 SQL。
    /// </summary>
    /// <param name="primaryKeyColumns">主键列集合。</param>
    /// <param name="tableName">表名。</param>
    /// <returns>主键 SQL 片段。</returns>
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

    /// <summary>
    /// 构建索引定义 SQL 片段。
    /// </summary>
    /// <param name="index">索引模板。</param>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="tableName">表名。</param>
    /// <param name="fullName">Schema 全限定表名。</param>
    /// <returns>索引定义 SQL。</returns>
    private static string BuildIndexSql(ShardIndexSchema index, string logicalTable, string tableName, string fullName)
    {
        var uniqueSql = index.IsUnique ? "UNIQUE " : string.Empty;
        var indexName = ShardSchemaTemplateBuilder.BuildPhysicalIndexName(logicalTable, tableName, index.DatabaseName);
        var columnsSql = string.Join(", ", index.ColumnNames.Select(column => $"[{column}]"));
        return $"    CREATE {uniqueSql}INDEX [{indexName}] ON {fullName}({columnsSql});";
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
}
