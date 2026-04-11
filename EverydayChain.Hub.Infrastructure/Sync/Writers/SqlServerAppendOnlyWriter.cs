using System.Data;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;

namespace EverydayChain.Hub.Infrastructure.Sync.Writers;

/// <summary>
/// SQL Server 仅追加写入器。
/// </summary>
public class SqlServerAppendOnlyWriter(
    IOptions<ShardingOptions> shardingOptions,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<SqlServerAppendOnlyWriter> logger) : ISqlServerAppendOnlyWriter {

    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _shardingOptions = shardingOptions.Value;
    /// <summary>按逻辑表+后缀缓存分表就绪任务，避免每页重复执行分表确认。</summary>
    private readonly ConcurrentDictionary<string, Lazy<Task>> _shardReadyTasks = new(StringComparer.Ordinal);

    /// <summary>状态驱动追加写入超时秒数。</summary>
    private const int AppendTimeoutSeconds = 180;
    /// <summary>状态驱动追加写入慢调用阈值（毫秒）。</summary>
    private const int SlowAppendWarningThresholdMs = 3000;

    /// <inheritdoc/>
    public async Task<int> AppendAsync(
        string tableCode,
        string targetLogicalTable,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct) {
        if (rows.Count == 0) {
            return 0;
        }

        EnsureSafeIdentifier(targetLogicalTable, nameof(targetLogicalTable));
        var shardSuffix = ResolveShardSuffix();
        await EnsureShardPreparedOnceAsync(targetLogicalTable, shardSuffix, ct);
        var destination = BuildTargetTableFullName(targetLogicalTable, shardSuffix);
        var payload = BuildPayloadDataTable(rows);
        return await dangerZoneExecutor.ExecuteAsync(
           $"SqlServerStatusAppend:{tableCode}",
           token => AppendCoreAsync(tableCode, destination, payload, token),
           ct,
           AppendTimeoutSeconds);
    }

    /// <summary>
    /// 执行真实 SQL Server 批量追加写入。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="destination">目标全表名。</param>
    /// <param name="payload">待写入数据。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>写入行数。</returns>
    private async Task<int> AppendCoreAsync(string tableCode, string destination, DataTable payload, CancellationToken ct) {
        var appendStopwatch = System.Diagnostics.Stopwatch.StartNew();
        await using var connection = new SqlConnection(_shardingOptions.ConnectionString);
        await connection.OpenAsync(ct);
        using var bulkCopy = new SqlBulkCopy(connection) {
            DestinationTableName = destination,
            BatchSize = Math.Min(payload.Rows.Count, 2000),
            BulkCopyTimeout = AppendTimeoutSeconds,
            EnableStreaming = true,
        };

        foreach (DataColumn column in payload.Columns) {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(payload, ct);
        appendStopwatch.Stop();
        logger.LogInformation(
            "状态驱动追加写入完成。TableCode={TableCode}, TargetTable={TargetTable}, Rows={Rows}, AppendElapsedMs={AppendElapsedMs}",
            tableCode,
            destination,
            payload.Rows.Count,
            appendStopwatch.ElapsedMilliseconds);
        if (appendStopwatch.ElapsedMilliseconds >= SlowAppendWarningThresholdMs) {
            logger.LogWarning(
                "状态驱动追加写入耗时较高。TableCode={TableCode}, TargetTable={TargetTable}, Rows={Rows}, AppendElapsedMs={AppendElapsedMs}, SlowThresholdMs={SlowThresholdMs}",
                tableCode,
                destination,
                payload.Rows.Count,
                appendStopwatch.ElapsedMilliseconds,
                SlowAppendWarningThresholdMs);
        }
        return payload.Rows.Count;
    }

    /// <summary>
    /// 按分表后缀确保一次分表存在性检查，降低分页写入链路元数据开销。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="shardSuffix">分表后缀。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task EnsureShardPreparedOnceAsync(string logicalTable, string shardSuffix, CancellationToken ct) {
        var cacheKey = $"{logicalTable}:{shardSuffix}";
        var lazyTask = _shardReadyTasks.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task>(
                () => shardTableProvisioner.EnsureShardTableAsync(logicalTable, shardSuffix, CancellationToken.None),
                LazyThreadSafetyMode.ExecutionAndPublication));
        try {
            await lazyTask.Value.WaitAsync(ct);
        }
        catch {
            _shardReadyTasks.TryRemove(cacheKey, out _);
            throw;
        }
    }

    /// <summary>
    /// 构建批量追加数据表。
    /// </summary>
    /// <param name="rows">行集合。</param>
    /// <returns>数据表。</returns>
    private static DataTable BuildPayloadDataTable(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows) {
        var firstRow = rows[0];
        var columnNames = firstRow.Keys
            .Where(name => !string.Equals(name, "__RowId", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (columnNames.Count == 0) {
            throw new InvalidOperationException("状态驱动追加写入失败：可写入列为空。 ");
        }

        var table = new DataTable();
        foreach (var columnName in columnNames) {
            table.Columns.Add(columnName, typeof(object));
        }

        foreach (var row in rows) {
            var dataRow = table.NewRow();
            foreach (var columnName in columnNames) {
                dataRow[columnName] = row.TryGetValue(columnName, out var value) && value is not null ? value : DBNull.Value;
            }

            table.Rows.Add(dataRow);
        }

        return table;
    }

    /// <summary>
    /// 解析分表后缀。
    /// </summary>
    /// <returns>分表后缀。</returns>
    private string ResolveShardSuffix() {
        var nowLocal = DateTimeOffset.Now;
        var suffixes = AutoMigrationService.BuildBootstrapSuffixes(
            shardSuffixResolver,
            nowLocal,
            _shardingOptions.AutoCreateMonthsAhead);
        return suffixes[0];
    }

    /// <summary>
    /// 构建目标全名。
    /// </summary>
    /// <param name="targetLogicalTable">逻辑表名。</param>
    /// <param name="shardSuffix">分表后缀。</param>
    /// <returns>全名。</returns>
    private string BuildTargetTableFullName(string targetLogicalTable, string shardSuffix) {
        EnsureSafeIdentifier(_shardingOptions.Schema, nameof(_shardingOptions.Schema));
        var physicalTable = $"{targetLogicalTable}{shardSuffix}";
        return $"[{EscapeIdentifier(_shardingOptions.Schema)}].[{EscapeIdentifier(physicalTable)}]";
    }

    /// <summary>
    /// 转义标识符。
    /// </summary>
    /// <param name="identifier">标识符。</param>
    /// <returns>转义文本。</returns>
    private static string EscapeIdentifier(string identifier) {
        return identifier.Replace("]", "]]", StringComparison.Ordinal);
    }

    /// <summary>
    /// 标识符安全校验。
    /// </summary>
    /// <param name="identifier">标识符。</param>
    /// <param name="fieldName">字段名。</param>
    private static void EnsureSafeIdentifier(string identifier, string fieldName) {
        if (string.IsNullOrWhiteSpace(identifier) || !identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_')) {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。 ");
        }
    }
}
