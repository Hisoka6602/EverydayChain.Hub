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
    /// <summary>分表就绪缓存最大条目数，超出时清理已完成的旧条目，防止长时间运行进程内存单调增长。</summary>
    private const int MaxShardReadyCacheEntries = 200;
    /// <summary>分表就绪缓存清理目标条目数，清理时保留至此数量以减少频繁触发清理。</summary>
    private const int TargetShardReadyCacheEntries = 100;

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
    /// 按逻辑表+后缀确保分表存在性仅检查一次，降低分页写入链路元数据开销。
    /// 调用方取消等待不会移除缓存键；仅在分表确认任务自身出错或被取消时才清除缓存以允许重试。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="shardSuffix">分表后缀。</param>
    /// <param name="ct">取消令牌，传递给首次分表确认调用；后续等待者仅用于等待超时控制。</param>
    private async Task EnsureShardPreparedOnceAsync(string logicalTable, string shardSuffix, CancellationToken ct) {
        var cacheKey = $"{logicalTable}:{shardSuffix}";
        var lazyTask = _shardReadyTasks.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task>(
                () => shardTableProvisioner.EnsureShardTableAsync(logicalTable, shardSuffix, ct),
                LazyThreadSafetyMode.ExecutionAndPublication));
        TrimShardReadyCacheIfNeeded();
        try {
            await lazyTask.Value.WaitAsync(ct);
        }
        catch {
            // 仅在分表确认任务自身出错或取消时移除缓存，允许后续请求重试。
            // 调用方取消等待（OperationCanceledException 来自 ct）但任务仍在运行时，保留缓存键。
            if (lazyTask.Value.IsFaulted || lazyTask.Value.IsCanceled) {
                _shardReadyTasks.TryRemove(cacheKey, out _);
            }
            throw;
        }
    }

    /// <summary>
    /// 当分表就绪缓存超过上限时，优先清理已成功完成的旧条目，防止长时间运行进程内存单调增长。
    /// 仅在条目数超过 <see cref="MaxShardReadyCacheEntries"/> 时触发清理，以降低热路径中的遍历频率。
    /// 多线程可能同时触发此方法，但 <see cref="ConcurrentDictionary{TKey,TValue}"/> 保证
    /// <see cref="ConcurrentDictionary{TKey,TValue}.TryRemove(TKey,out TValue)"/> 是线程安全的；
    /// 即使多线程并发过度清理，分表确认为幂等操作，不影响正确性。
    /// </summary>
    private void TrimShardReadyCacheIfNeeded() {
        if (_shardReadyTasks.Count <= MaxShardReadyCacheEntries) return;
        var excess = _shardReadyTasks.Count - TargetShardReadyCacheEntries;
        var toRemove = _shardReadyTasks
            .Where(kv => kv.Value.IsValueCreated && kv.Value.Value.IsCompletedSuccessfully)
            .Select(kv => kv.Key)
            .Take(excess)
            .ToList();
        foreach (var key in toRemove) {
            _shardReadyTasks.TryRemove(key, out _);
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
