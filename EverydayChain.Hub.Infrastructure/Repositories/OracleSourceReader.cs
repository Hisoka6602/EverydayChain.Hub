using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.SharedKernel.Utilities;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// Oracle 源端读取器实现（真实 Oracle 只读查询）。
/// </summary>
public class OracleSourceReader(IOptions<OracleOptions> oracleOptions, ILogger<OracleSourceReader> logger) : IOracleSourceReader
{
    /// <summary>默认分页上限。</summary>
    private const int DefaultMaxPageSize = 5000;

    /// <summary>默认命令超时秒数。</summary>
    private const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>Oracle 配置快照。</summary>
    private readonly OracleOptions _options = oracleOptions.Value;

    /// <inheritdoc/>
    public async Task<SyncReadResult> ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct)
    {
        try
        {
            var sourceSchema = ResolveSourceSchema(request.SourceSchema);
            ValidateReadRequest(request, sourceSchema);
            var pageSize = ResolvePageSize(request.PageSize);
            var offset = (request.PageNo - 1) * pageSize;
            var limit = offset + pageSize;
            var sql = BuildReadPageSql(request, sourceSchema);

            await using var connection = new OracleConnection(_options.ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandTimeout = ResolveCommandTimeout();
            command.CommandText = sql;
            command.Parameters.Add("p_windowStart", OracleDbType.TimeStamp, request.Window.WindowStartLocal, ParameterDirection.Input);
            command.Parameters.Add("p_windowEnd", OracleDbType.TimeStamp, request.Window.WindowEndLocal, ParameterDirection.Input);
            command.Parameters.Add("p_offset", OracleDbType.Int32, offset, ParameterDirection.Input);
            command.Parameters.Add("p_limit", OracleDbType.Int32, limit, ParameterDirection.Input);
            EnsureReadOnlyCommand(command);

            var rows = new List<IReadOnlyDictionary<string, object?>>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
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

                rows.Add(SyncColumnFilter.FilterExcludedColumns(row, request.NormalizedExcludedColumns));
            }

            return new SyncReadResult
            {
                Rows = rows,
            };
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Oracle 增量读取失败。TableCode={TableCode}, SourceSchema={SourceSchema}, SourceTable={SourceTable}, PageNo={PageNo}",
                request.TableCode,
                ResolveSourceSchemaForLog(request.SourceSchema),
                request.SourceTable,
                request.PageNo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct)
    {
        try
        {
            var sourceSchema = ResolveSourceSchema(request.SourceSchema);
            ValidateReadKeyRequest(request, sourceSchema);
            if (request.UniqueKeys.Count == 0)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var sql = BuildReadKeysSql(request, sourceSchema);
            await using var connection = new OracleConnection(_options.ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandTimeout = ResolveCommandTimeout();
            command.CommandText = sql;
            command.Parameters.Add("p_windowStart", OracleDbType.TimeStamp, request.Window.WindowStartLocal, ParameterDirection.Input);
            command.Parameters.Add("p_windowEnd", OracleDbType.TimeStamp, request.Window.WindowEndLocal, ParameterDirection.Input);
            EnsureReadOnlyCommand(command);

            var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object?>(request.UniqueKeys.Count, StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                }

                var key = SyncBusinessKeyBuilder.Build(row, request.UniqueKeys);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    keySet.Add(key);
                }
            }

            return keySet;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Oracle 业务键读取失败。TableCode={TableCode}, SourceSchema={SourceSchema}, SourceTable={SourceTable}",
                request.TableCode,
                ResolveSourceSchemaForLog(request.SourceSchema),
                request.SourceTable);
            throw;
        }
    }

    /// <summary>
    /// 构建分页读取 SQL。
    /// 前置条件：入参标识符已通过 <see cref="ValidateSafeIdentifier"/> 校验，禁止移除该前置校验。
    /// </summary>
    /// <param name="request">读取请求。</param>
    /// <param name="sourceSchema">生效源端 Schema。</param>
    /// <returns>SQL 文本。</returns>
    private static string BuildReadPageSql(SyncReadRequest request, string sourceSchema)
    {
        var orderColumns = new List<string> { $"t.{request.CursorColumn}" };
        foreach (var key in request.UniqueKeys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                orderColumns.Add($"t.{key}");
            }
        }

        if (orderColumns.Count == 1)
        {
            // 当未配置 UniqueKeys 时, 使用 Oracle ROWID 作为分页稳定排序兜底列, 避免窗口内重复/漏读。
            orderColumns.Add("t.ROWID");
        }

        var orderClause = string.Join(", ", orderColumns);
        return $"""
                SELECT *
                FROM (
                    SELECT t.*, ROW_NUMBER() OVER (ORDER BY {orderClause}) AS RN
                    FROM {sourceSchema}.{request.SourceTable} t
                    WHERE t.{request.CursorColumn} > :p_windowStart
                      AND t.{request.CursorColumn} <= :p_windowEnd
                ) q
                WHERE q.RN > :p_offset
                  AND q.RN <= :p_limit
                ORDER BY q.RN
                """;
    }

    /// <summary>
    /// 构建业务键读取 SQL。
    /// 前置条件：入参标识符已通过 <see cref="ValidateSafeIdentifier"/> 校验，禁止移除该前置校验。
    /// </summary>
    /// <param name="request">业务键读取请求。</param>
    /// <param name="sourceSchema">生效源端 Schema。</param>
    /// <returns>SQL 文本。</returns>
    /// <exception cref="InvalidOperationException">当唯一键列为空时抛出。</exception>
    private static string BuildReadKeysSql(SyncKeyReadRequest request, string sourceSchema)
    {
        var validUniqueKeys = request.UniqueKeys.Where(key => !string.IsNullOrWhiteSpace(key)).ToList();
        if (validUniqueKeys.Count == 0)
        {
            throw new InvalidOperationException($"表 {request.TableCode} 的 UniqueKeys 不能为空。");
        }

        var selectColumns = string.Join(", ", validUniqueKeys.Select(key => $"t.{key}"));
        return $"""
                SELECT {selectColumns}
                FROM {sourceSchema}.{request.SourceTable} t
                WHERE t.{request.CursorColumn} > :p_windowStart
                  AND t.{request.CursorColumn} <= :p_windowEnd
                """;
    }

    /// <summary>
    /// 校验分页读取请求。
    /// </summary>
    /// <param name="request">读取请求。</param>
    /// <param name="sourceSchema">生效源端 Schema。</param>
    /// <exception cref="InvalidOperationException">当连接配置或标识符非法时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当分页参数小于等于 0 时抛出。</exception>
    private void ValidateReadRequest(SyncReadRequest request, string sourceSchema)
    {
        ValidateConnectionOptions();
        EnsureReadOnlyRequest(sourceSchema, request.SourceTable, request.CursorColumn, request.UniqueKeys);
        if (request.PageNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageNo), request.PageNo, "PageNo 必须大于 0。");
        }

        if (request.PageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageSize), request.PageSize, "PageSize 必须大于 0。");
        }
    }

    /// <summary>
    /// 校验业务键读取请求。
    /// </summary>
    /// <param name="request">业务键读取请求。</param>
    /// <param name="sourceSchema">生效源端 Schema。</param>
    /// <exception cref="InvalidOperationException">当连接配置或标识符非法时抛出。</exception>
    private void ValidateReadKeyRequest(SyncKeyReadRequest request, string sourceSchema)
    {
        ValidateConnectionOptions();
        EnsureReadOnlyRequest(sourceSchema, request.SourceTable, request.CursorColumn, request.UniqueKeys);
    }

    /// <summary>
    /// 解析源端 Schema。
    /// </summary>
    /// <param name="sourceSchema">请求 Schema。</param>
    /// <returns>生效 Schema。</returns>
    /// <exception cref="InvalidOperationException">当请求值与默认配置均为空时抛出。</exception>
    private string ResolveSourceSchema(string sourceSchema)
    {
        if (!string.IsNullOrWhiteSpace(sourceSchema))
        {
            return sourceSchema;
        }

        if (!string.IsNullOrWhiteSpace(_options.DefaultSchema))
        {
            return _options.DefaultSchema;
        }

        throw new InvalidOperationException("SourceSchema 为空且 Oracle.DefaultSchema 未配置。");
    }

    /// <summary>
    /// 校验 Oracle 连接配置。
    /// </summary>
    /// <exception cref="InvalidOperationException">当连接字符串为空时抛出。</exception>
    private void ValidateConnectionOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Oracle.ConnectionString 不能为空。");
        }
    }

    /// <summary>
    /// 解析分页大小。
    /// </summary>
    /// <param name="requestedPageSize">请求分页大小。</param>
    /// <returns>生效分页大小。</returns>
    private int ResolvePageSize(int requestedPageSize)
    {
        var maxPageSize = _options.MaxPageSize > 0 ? _options.MaxPageSize : DefaultMaxPageSize;
        if (requestedPageSize <= maxPageSize)
        {
            return requestedPageSize;
        }

        logger.LogWarning(
            "分页大小超过上限，已自动截断。RequestedPageSize={RequestedPageSize}, MaxPageSize={MaxPageSize}",
            requestedPageSize,
            maxPageSize);
        return maxPageSize;
    }

    /// <summary>
    /// 解析命令超时。
    /// </summary>
    /// <returns>命令超时秒数。</returns>
    private int ResolveCommandTimeout()
    {
        return _options.CommandTimeoutSeconds > 0 ? _options.CommandTimeoutSeconds : DefaultCommandTimeoutSeconds;
    }

    /// <summary>
    /// 解析日志使用的源端 Schema。
    /// </summary>
    /// <param name="sourceSchema">请求 Schema。</param>
    /// <returns>可用于日志的 Schema 文本。</returns>
    private string ResolveSourceSchemaForLog(string sourceSchema)
    {
        if (!string.IsNullOrWhiteSpace(sourceSchema))
        {
            return sourceSchema;
        }

        if (!string.IsNullOrWhiteSpace(_options.DefaultSchema))
        {
            return _options.DefaultSchema;
        }

        return "未配置";
    }

    /// <summary>
    /// 强制只读命令校验.
    /// </summary>
    /// <param name="command">命令对象。</param>
    /// <exception cref="InvalidOperationException">当命令不是 SELECT 且启用只读约束时抛出。</exception>
    private void EnsureReadOnlyCommand(OracleCommand command)
    {
        if (!_options.ReadOnly)
        {
            return;
        }

        var commandText = command.CommandText.TrimStart();
        if (!commandText.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Oracle 读取器仅允许执行 SELECT 语句。");
        }
    }

    /// <summary>
    /// 校验源端只读请求对象名，阻断 DDL/DML 注入风险。
    /// </summary>
    /// <param name="sourceSchema">源端 Schema。</param>
    /// <param name="sourceTable">源端表名。</param>
    /// <param name="cursorColumn">游标列名。</param>
    /// <param name="uniqueKeys">唯一键集合。</param>
    /// <exception cref="InvalidOperationException">当对象名包含危险字符时抛出。</exception>
    private static void EnsureReadOnlyRequest(
        string sourceSchema,
        string sourceTable,
        string cursorColumn,
        IReadOnlyList<string> uniqueKeys)
    {
        ValidateSafeIdentifier(sourceSchema, nameof(sourceSchema));
        ValidateSafeIdentifier(sourceTable, nameof(sourceTable));
        ValidateSafeIdentifier(cursorColumn, nameof(cursorColumn));
        for (var index = 0; index < uniqueKeys.Count; index++)
        {
            var uniqueKey = uniqueKeys[index];
            if (string.IsNullOrWhiteSpace(uniqueKey))
            {
                continue;
            }

            ValidateSafeIdentifier(uniqueKey, $"uniqueKeys[{index}]");
        }
    }

    /// <summary>
    /// 校验对象名仅包含安全字符。
    /// </summary>
    /// <param name="identifier">对象名。</param>
    /// <param name="fieldName">字段名。</param>
    /// <exception cref="InvalidOperationException">当对象名非法时抛出。</exception>
    private static void ValidateSafeIdentifier(string identifier, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException($"{fieldName} 不能为空。");
        }

        if (!identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。");
        }
    }
}
