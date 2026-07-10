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
/// 定义 OracleStatusDrivenSourceReader 类型。
/// </summary>
public class OracleStatusDrivenSourceReader(
    IOptions<OracleOptions> oracleOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<OracleStatusDrivenSourceReader> logger) : IOracleStatusDrivenSourceReader {

    /// <summary>
    /// 存储 DefaultCommandTimeoutSeconds 字段。
    /// </summary>
    private const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly OracleOptions _options = oracleOptions.Value;

    private readonly string _effectiveConnectionString = BuildConnectionString(oracleOptions.Value);

    /// <summary>
    /// 执行 ReadPendingPageAsync 方法。
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadPendingPageAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        int pageNo,
        int pageSize,
        IReadOnlySet<string> normalizedExcludedColumns,
        SyncWindow window,
        CancellationToken ct) {
            // 步骤：执行 BuildConnectionString 方法的核心处理流程。
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
        var whereClause = BuildWhereClause(definition, profile, hasCursorFilter);
        var statusPredicateMode = ResolveStatusPredicateMode(profile);
        var windowStartLocal = hasCursorFilter ? window.WindowStartLocal : (DateTime?)null;
        var windowEndLocal = hasCursorFilter ? window.WindowEndLocal : (DateTime?)null;
        var sql = BuildReadSql(definition, profile, hasCursorFilter);
        logger.LogInformation(
            "Oracle 状态驱动读取计划。TableCode={TableCode}, SourceSchema={SourceSchema}, SourceTable={SourceTable}, PageNo={PageNo}, RequestedPageSize={RequestedPageSize}, EffectivePageSize={EffectivePageSize}, Offset={Offset}, Limit={Limit}, HasCursorFilter={HasCursorFilter}, WindowStartLocal={WindowStartLocal}, WindowEndLocal={WindowEndLocal}, StatusPredicateMode={StatusPredicateMode}, PendingStatusValue={PendingStatusValue}, IgnorePendingStatusValue={IgnorePendingStatusValue}, ShouldWriteBackRemoteStatus={ShouldWriteBackRemoteStatus}, OracleReadOnly={OracleReadOnly}, WhereClause={WhereClause}",
            definition.TableCode,
            definition.SourceSchema,
            definition.SourceTable,
            pageNo,
            pageSize,
            effectivePageSize,
            offset,
            limit,
            hasCursorFilter,
            windowStartLocal,
            windowEndLocal,
            statusPredicateMode,
            profile.PendingStatusValue,
            profile.IgnorePendingStatusValue,
            profile.ShouldWriteBackRemoteStatus,
            _options.ReadOnly,
            whereClause);
        return await dangerZoneExecutor.ExecuteAsync(
            $"OracleStatusDrivenRead:{definition.TableCode}:P{pageNo}",
            /// <summary>
            /// 获取或设置 token。
            /// </summary>
            async token => {
                await using var connection = new OracleConnection(_effectiveConnectionString);
                await connection.OpenAsync(token);
                await using var command = connection.CreateCommand();
                command.BindByName = true;
                command.CommandTimeout = ResolveCommandTimeout();
                command.CommandText = sql;
                command.Parameters.Add("p_offset", OracleDbType.Int32, offset, ParameterDirection.Input);
                command.Parameters.Add("p_limit", OracleDbType.Int32, limit, ParameterDirection.Input);
                if (!profile.IgnorePendingStatusValue && profile.PendingStatusValue is not null) {
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

                logger.LogInformation(
                    "Oracle 状态驱动读取完成。TableCode={TableCode}, SourceSchema={SourceSchema}, SourceTable={SourceTable}, PageNo={PageNo}, RowCount={RowCount}, StatusPredicateMode={StatusPredicateMode}, HasCursorFilter={HasCursorFilter}",
                    definition.TableCode,
                    definition.SourceSchema,
                    definition.SourceTable,
                    pageNo,
                    rows.Count,
                    statusPredicateMode,
                    hasCursorFilter);
                return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)rows;
            },
            ct);
    }

    /// <summary>
    /// 执行 BuildReadSql 方法。
    /// </summary>
    private static string BuildReadSql(SyncTableDefinition definition, RemoteStatusConsumeProfile profile, bool hasCursorFilter) {
        var whereClause = BuildWhereClause(definition, profile, hasCursorFilter);
        return BuildReadSql(definition, whereClause);
    }

    private static string BuildReadSql(SyncTableDefinition definition, string whereClause)
    {
        return $"""
SELECT *
FROM (
    SELECT
        t.*,
        ROWID AS "__RowId",
        ROW_NUMBER() OVER (ORDER BY ROWID) AS RN
    FROM {definition.SourceSchema}.{definition.SourceTable} t
    WHERE {whereClause}
)
WHERE RN > :p_offset AND RN <= :p_limit
ORDER BY RN
""";
    }

    private static string BuildWhereClause(SyncTableDefinition definition, RemoteStatusConsumeProfile profile, bool hasCursorFilter)
    {
        // 步骤：执行 BuildReadSql 方法的核心处理流程。
        var statusPredicate = profile.IgnorePendingStatusValue
            ? string.Empty
            : profile.PendingStatusValue is null
                ? $"{profile.StatusColumnName} IS NULL"
                : $"{profile.StatusColumnName} = :p_pendingStatus";
        var cursorPredicate = hasCursorFilter
            ? $"{definition.CursorColumn} >= :p_windowStart AND {definition.CursorColumn} <= :p_windowEnd"
            : string.Empty;
        var predicates = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(statusPredicate))
        {
            predicates.Add(statusPredicate);
        }

        if (!string.IsNullOrWhiteSpace(cursorPredicate))
        {
            predicates.Add(cursorPredicate);
        }

        return predicates.Count == 0 ? "1 = 1" : string.Join(" AND ", predicates);
    }

    private static string ResolveStatusPredicateMode(RemoteStatusConsumeProfile profile)
    {
        if (profile.IgnorePendingStatusValue)
        {
            return "Ignore";
        }

        return profile.PendingStatusValue is null ? "IsNull" : "Equals";
    }

    /// <summary>
    /// 执行 ResolvePageSize 方法。
    /// </summary>
    private int ResolvePageSize(int requestedPageSize) {
        // 步骤：执行 ResolvePageSize 方法的核心处理流程。
        var maxPageSize = _options.MaxPageSize > 0 ? _options.MaxPageSize : 5000;
        if (requestedPageSize <= maxPageSize) {
            return requestedPageSize;
        }

        logger.LogWarning("状态驱动读取分页大小超过上限，已自动截断。RequestedPageSize={RequestedPageSize}, MaxPageSize={MaxPageSize}", requestedPageSize, maxPageSize);
        return maxPageSize;
    }

    /// <summary>
    /// 执行 ResolveCommandTimeout 方法。
    /// </summary>
    private int ResolveCommandTimeout() {
        // 步骤：执行 ResolveCommandTimeout 方法的核心处理流程。
        return _options.CommandTimeoutSeconds > 0 ? _options.CommandTimeoutSeconds : DefaultCommandTimeoutSeconds;
    }

    /// <summary>
    /// 执行 EnsureReadOnlyCommand 方法。
    /// </summary>
    private void EnsureReadOnlyCommand(OracleCommand command) {
        // 步骤：执行 EnsureReadOnlyCommand 方法的核心处理流程。
        if (!_options.ReadOnly) {
            return;
        }

        var commandText = command.CommandText.TrimStart();
        if (!commandText.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("Oracle 状态驱动读取器仅允许执行 SELECT 语句。 ");
        }
    }

    /// <summary>
    /// 执行 EnsureSafeIdentifier 方法。
    /// </summary>
    private static void EnsureSafeIdentifier(string identifier, string fieldName) {
        // 步骤：执行 EnsureSafeIdentifier 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(identifier) || !identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_')) {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。 ");
        }
    }

    /// <summary>
    /// 执行 BuildConnectionString 方法。
    /// </summary>
    private static string BuildConnectionString(OracleOptions options) {
        // 步骤：执行 BuildConnectionString 方法的核心处理流程。
        return OracleConnectionStringResolver.BuildEffectiveConnectionString(options);
    }
}

