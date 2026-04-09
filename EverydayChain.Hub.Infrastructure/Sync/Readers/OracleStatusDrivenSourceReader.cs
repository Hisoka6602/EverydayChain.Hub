using System.Data;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Infrastructure.Sync.Abstractions;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace EverydayChain.Hub.Infrastructure.Sync.Readers;

/// <summary>
/// Oracle 状态驱动读取器。
/// 按状态列筛选待处理数据并返回包含 <c>__RowId</c> 的结果行。
/// </summary>
public class OracleStatusDrivenSourceReader(
    IOptions<OracleOptions> oracleOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<OracleStatusDrivenSourceReader> logger) : IOracleStatusDrivenSourceReader
{
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
        CancellationToken ct)
    {
        EnsureSafeIdentifier(definition.SourceSchema, nameof(definition.SourceSchema));
        EnsureSafeIdentifier(definition.SourceTable, nameof(definition.SourceTable));
        EnsureSafeIdentifier(profile.StatusColumnName, nameof(profile.StatusColumnName));
        if (pageNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNo), "页码必须大于 0。 ");
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "分页大小必须大于 0。 ");
        }

        var effectivePageSize = ResolvePageSize(pageSize);
        var offset = (pageNo - 1) * effectivePageSize;
        var limit = offset + effectivePageSize;
        var sql = BuildReadSql(definition, profile);
        return await dangerZoneExecutor.ExecuteAsync(
            $"OracleStatusDrivenRead:{definition.TableCode}:P{pageNo}",
            async token =>
            {
                await using var connection = new OracleConnection(_effectiveConnectionString);
                await connection.OpenAsync(token);
                await using var command = connection.CreateCommand();
                command.BindByName = true;
                command.CommandTimeout = ResolveCommandTimeout();
                command.CommandText = sql;
                command.Parameters.Add("p_offset", OracleDbType.Int32, offset, ParameterDirection.Input);
                command.Parameters.Add("p_limit", OracleDbType.Int32, limit, ParameterDirection.Input);
                if (profile.PendingStatusValue is not null)
                {
                    command.Parameters.Add("p_pendingStatus", OracleDbType.Varchar2, profile.PendingStatusValue, ParameterDirection.Input);
                }

                EnsureReadOnlyCommand(command);

                var rows = new List<IReadOnlyDictionary<string, object?>>();
                await using var reader = await command.ExecuteReaderAsync(token);
                while (await reader.ReadAsync(token))
                {
                    var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        var name = reader.GetName(index);
                        if (string.Equals(name, "RN", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        row[name] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                    }

                    var filtered = SyncColumnFilter.FilterExcludedColumns(row, normalizedExcludedColumns);
                    if (!filtered.ContainsKey("__RowId") && row.TryGetValue("__RowId", out var rowIdValue))
                    {
                        var mutable = new Dictionary<string, object?>(filtered, StringComparer.OrdinalIgnoreCase)
                        {
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
    /// <returns>参数化 SQL。</returns>
    private static string BuildReadSql(SyncTableDefinition definition, RemoteStatusConsumeProfile profile)
    {
        var statusPredicate = profile.PendingStatusValue is null
            ? $"{profile.StatusColumnName} IS NULL"
            : $"{profile.StatusColumnName} = :p_pendingStatus";
        return $"""
SELECT *
FROM (
    SELECT
        t.*,
        ROWID AS "__RowId",
        ROW_NUMBER() OVER (ORDER BY ROWID) AS RN
    FROM {definition.SourceSchema}.{definition.SourceTable} t
    WHERE {statusPredicate}
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
    private int ResolvePageSize(int requestedPageSize)
    {
        var maxPageSize = _options.MaxPageSize > 0 ? _options.MaxPageSize : 5000;
        if (requestedPageSize <= maxPageSize)
        {
            return requestedPageSize;
        }

        logger.LogWarning("状态驱动读取分页大小超过上限，已自动截断。RequestedPageSize={RequestedPageSize}, MaxPageSize={MaxPageSize}", requestedPageSize, maxPageSize);
        return maxPageSize;
    }

    /// <summary>
    /// 解析命令超时秒数。
    /// </summary>
    /// <returns>超时秒数。</returns>
    private int ResolveCommandTimeout()
    {
        return _options.CommandTimeoutSeconds > 0 ? _options.CommandTimeoutSeconds : DefaultCommandTimeoutSeconds;
    }

    /// <summary>
    /// 只读命令校验。
    /// </summary>
    /// <param name="command">命令对象。</param>
    private void EnsureReadOnlyCommand(OracleCommand command)
    {
        if (!_options.ReadOnly)
        {
            return;
        }

        var commandText = command.CommandText.TrimStart();
        if (!commandText.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Oracle 状态驱动读取器仅允许执行 SELECT 语句。 ");
        }
    }

    /// <summary>
    /// 校验安全标识符。
    /// </summary>
    /// <param name="identifier">标识符。</param>
    /// <param name="fieldName">字段名。</param>
    private static void EnsureSafeIdentifier(string identifier, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。 ");
        }
    }

    /// <summary>
    /// 构建生效连接串。
    /// </summary>
    /// <param name="options">Oracle 配置。</param>
    /// <returns>连接串。</returns>
    private static string BuildConnectionString(OracleOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Oracle.ConnectionString 不能为空。 ");
        }

        return options.ConnectionString;
    }
}
