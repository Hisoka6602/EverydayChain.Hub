using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Domain.Sync;
using Oracle.ManagedDataAccess.Client;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;

namespace EverydayChain.Hub.Infrastructure.Sync.Readers;

/// <summary>
/// Oracle 状态驱动读取器。
/// 按状态列筛选待处理数据并返回包含 <c>__RowId</c> 的结果行。
/// </summary>
public class OracleStatusDrivenSourceReader(
    IOptions<OracleOptions> oracleOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<OracleStatusDrivenSourceReader> logger) : IOracleStatusDrivenSourceReader {

    /// <summary>默认命令超时秒数。</summary>
    private const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>Oracle 配置快照。</summary>
    private readonly OracleOptions _options = oracleOptions.Value;

    /// <summary>生效连接字符串。</summary>
    private readonly string _effectiveConnectionString = BuildConnectionString(oracleOptions.Value);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadPendingPageAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        int pageNo,
        int pageSize,
        IReadOnlySet<string> normalizedExcludedColumns,
        SyncWindow window,
        CancellationToken ct) {
        EnsureSafeIdentifier(definition.SourceSchema, nameof(definition.SourceSchema));
        EnsureSafeIdentifier(definition.SourceTable, nameof(definition.SourceTable));
        EnsureSafeIdentifier(profile.StatusColumnName, nameof(profile.StatusColumnName));
        var hasCursorFilter = !string.IsNullOrWhiteSpace(definition.CursorColumn);
        if (hasCursorFilter) {
            EnsureSafeIdentifier(definition.CursorColumn, nameof(definition.CursorColumn));
        }

        if (pageNo <= 0) {
            throw new ArgumentOutOfRangeException(nameof(pageNo), "页码必须大于 0。 ");
        }

        if (pageSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "分页大小必须大于 0。 ");
        }

        var effectivePageSize = ResolvePageSize(pageSize);
        var offset = (pageNo - 1) * effectivePageSize;
        var limit = offset + effectivePageSize;
        var sql = BuildReadSql(definition, profile, hasCursorFilter);
        return await dangerZoneExecutor.ExecuteAsync(
            $"OracleStatusDrivenRead:{definition.TableCode}:P{pageNo}",
            async token => {
                await using var connection = new OracleConnection(_effectiveConnectionString);
                await connection.OpenAsync(token);
                await using var command = connection.CreateCommand();
                command.BindByName = true;
                command.CommandTimeout = ResolveCommandTimeout();
                command.CommandText = sql;
                command.Parameters.Add("p_offset", OracleDbType.Int32, offset, ParameterDirection.Input);
                command.Parameters.Add("p_limit", OracleDbType.Int32, limit, ParameterDirection.Input);
                if (profile.PendingStatusValue is not null) {
                    command.Parameters.Add("p_pendingStatus", OracleDbType.Varchar2, profile.PendingStatusValue, ParameterDirection.Input);
                }

                if (hasCursorFilter) {
                    command.Parameters.Add("p_windowStart", OracleDbType.TimeStamp, window.WindowStartLocal, ParameterDirection.Input);
                    command.Parameters.Add("p_windowEnd", OracleDbType.TimeStamp, window.WindowEndLocal, ParameterDirection.Input);
                }

                EnsureReadOnlyCommand(command);

                var rows = new List<IReadOnlyDictionary<string, object?>>();
                await using var reader = await command.ExecuteReaderAsync(token);
                while (await reader.ReadAsync(token)) {
                    var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                    for (var index = 0; index < reader.FieldCount; index++) {
                        var name = reader.GetName(index);
                        if (string.Equals(name, "RN", StringComparison.OrdinalIgnoreCase)) {
                            continue;
                        }

                        row[name] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                    }

                    var filtered = SyncColumnFilter.FilterExcludedColumns(row, normalizedExcludedColumns);
                    if (!filtered.ContainsKey("__RowId") && row.TryGetValue("__RowId", out var rowIdValue)) {
                        var mutable = new Dictionary<string, object?>(filtered, StringComparer.OrdinalIgnoreCase) {
                            ["__RowId"] = rowIdValue,
                        };
                        rows.Add(mutable);
                        continue;
                    }

                    rows.Add(filtered);
                }

                return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)rows;
            },
            ct);
    }

    /// <summary>
    /// 构建状态驱动读取 SQL。
    /// </summary>
    /// <param name="definition">同步表定义。</param>
    /// <param name="profile">状态消费配置。</param>
    /// <param name="hasCursorFilter">是否追加游标列时间范围过滤条件。</param>
    /// <returns>参数化 SQL。</returns>
    /// <remarks>
    /// 当 <paramref name="hasCursorFilter"/> 为 true 时，<see cref="SyncTableDefinition.CursorColumn"/>
    /// 已在调用方 <see cref="ReadPendingPageAsync"/> 中通过 <see cref="EnsureSafeIdentifier"/> 完成
    /// 字母/数字/下划线安全校验，可安全直接嵌入 SQL 标识符位置。
    /// </remarks>
    private static string BuildReadSql(SyncTableDefinition definition, RemoteStatusConsumeProfile profile, bool hasCursorFilter) {
        var statusPredicate = profile.PendingStatusValue is null
            ? $"{profile.StatusColumnName} IS NULL"
            : $"{profile.StatusColumnName} = :p_pendingStatus";
        var cursorPredicate = hasCursorFilter
            ? $" AND {definition.CursorColumn} >= :p_windowStart AND {definition.CursorColumn} <= :p_windowEnd"
            : string.Empty;
        return $"""
SELECT *
FROM (
    SELECT
        t.*,
        ROWID AS "__RowId",
        ROW_NUMBER() OVER (ORDER BY ROWID) AS RN
    FROM {definition.SourceSchema}.{definition.SourceTable} t
    WHERE {statusPredicate}{cursorPredicate}
)
WHERE RN > :p_offset AND RN <= :p_limit
ORDER BY RN
""";
    }

    /// <summary>
    /// 解析分页大小。
    /// </summary>
    /// <param name="requestedPageSize">请求分页大小。</param>
    /// <returns>生效分页大小。</returns>
    private int ResolvePageSize(int requestedPageSize) {
        var maxPageSize = _options.MaxPageSize > 0 ? _options.MaxPageSize : 5000;
        if (requestedPageSize <= maxPageSize) {
            return requestedPageSize;
        }

        logger.LogWarning("状态驱动读取分页大小超过上限，已自动截断。RequestedPageSize={RequestedPageSize}, MaxPageSize={MaxPageSize}", requestedPageSize, maxPageSize);
        return maxPageSize;
    }

    /// <summary>
    /// 解析命令超时秒数。
    /// </summary>
    /// <returns>超时秒数。</returns>
    private int ResolveCommandTimeout() {
        return _options.CommandTimeoutSeconds > 0 ? _options.CommandTimeoutSeconds : DefaultCommandTimeoutSeconds;
    }

    /// <summary>
    /// 只读命令校验。
    /// </summary>
    /// <param name="command">命令对象。</param>
    private void EnsureReadOnlyCommand(OracleCommand command) {
        if (!_options.ReadOnly) {
            return;
        }

        var commandText = command.CommandText.TrimStart();
        if (!commandText.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("Oracle 状态驱动读取器仅允许执行 SELECT 语句。 ");
        }
    }

    /// <summary>
    /// 校验安全标识符。
    /// </summary>
    /// <param name="identifier">标识符。</param>
    /// <param name="fieldName">字段名。</param>
    private static void EnsureSafeIdentifier(string identifier, string fieldName) {
        if (string.IsNullOrWhiteSpace(identifier) || !identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_')) {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。 ");
        }
    }

    /// <summary>
    /// 构建生效连接串。
    /// </summary>
    /// <param name="options">Oracle 配置。</param>
    /// <returns>连接串。</returns>
    private static string BuildConnectionString(OracleOptions options) {
        return OracleConnectionStringResolver.BuildEffectiveConnectionString(options);
    }
}
