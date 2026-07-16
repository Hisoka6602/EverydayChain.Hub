using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Application.Abstractions.Persistence;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义 OracleSourceReader 类型。
/// </summary>
public class OracleSourceReader(
    IOptions<OracleOptions> oracleOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<OracleSourceReader> logger) : IOracleSourceReader {

    /// <summary>
    /// 存储 DefaultMaxPageSize 字段。
    /// </summary>
    private const int DefaultMaxPageSize = 5000;

    /// <summary>
    /// 存储 DefaultCommandTimeoutSeconds 字段。
    /// </summary>
    private const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>
    /// 存储 MaxBusinessKeyBatchSize 字段。
    /// </summary>
    private const int MaxBusinessKeyBatchSize = 500;

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly OracleOptions _options = oracleOptions.Value;

    /// <summary>
    /// 保存合并配置后的 Oracle 连接字符串。
    /// </summary>
    private readonly string _effectiveConnectionString = BuildConnectionString(oracleOptions.Value);

    /// <summary>
    /// 执行 ReadIncrementalPageAsync 方法。
    /// </summary>
    public async Task<SyncReadResult> ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct) {
        // 步骤：执行 ReadIncrementalPageAsync 方法的核心处理流程。
        var sourceSchema = ResolveSourceSchema(request.SourceSchema);
        ValidateReadRequest(request, sourceSchema);
        var pageSize = ResolvePageSize(request.PageSize);

        try {
            return await dangerZoneExecutor.ExecuteAsync(
                $"OracleIncrementalRead:{request.TableCode}:P{request.PageNo}",
                async token => {
                    try {
                        var offset = (request.PageNo - 1) * pageSize;
                        var limit = offset + pageSize;
                        var sql = BuildReadPageSql(request, sourceSchema);

                        await using var connection = new OracleConnection(_effectiveConnectionString);
                        await connection.OpenAsync(token);
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

                            rows.Add(SyncColumnFilter.FilterExcludedColumns(row, request.NormalizedExcludedColumns));
                        }

                        return new SyncReadResult { Rows = rows };
                    }
                    catch (OracleException oracleException) when (IsNonRetryableOracleException(oracleException)) {
                        throw new NonRetryableDangerZoneException(
                            $"Oracle 增量读取出现不可重试错误（TableCode={request.TableCode}, SourceTable={request.SourceTable}, PageNo={request.PageNo}, ErrorCode=ORA-{oracleException.Number}）。请核对 Oracle ServiceName/SID 与监听注册状态。",
                            oracleException);
                    }
                },
                ct);
        }
        catch (Exception exception) {
            logger.LogError(
                exception,
                "Oracle 增量读取失败（含重试耗尽）。TableCode={TableCode}, SourceSchema={SourceSchema}, SourceTable={SourceTable}, PageNo={PageNo}",
                request.TableCode,
                ResolveSourceSchemaForLog(request.SourceSchema),
                request.SourceTable,
                request.PageNo);
            throw;
        }
    }

    /// <summary>
    /// 执行 ReadByKeysAsync 方法。
    /// </summary>
    public async Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct) {
        // 步骤：执行 ReadByKeysAsync 方法的核心处理流程。
        var sourceSchema = ResolveSourceSchema(request.SourceSchema);
        ValidateReadKeyRequest(request, sourceSchema);
        if (request.UniqueKeys.Count == 0) {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var sql = BuildReadKeysSql(request, sourceSchema);

        try {
            return await dangerZoneExecutor.ExecuteAsync(
                $"OracleKeyRead:{request.TableCode}",
                async token => {
                    try {
                        await using var connection = new OracleConnection(_effectiveConnectionString);
                        await connection.OpenAsync(token);
                        await using var command = connection.CreateCommand();
                        command.BindByName = true;
                        command.CommandTimeout = ResolveCommandTimeout();
                        command.CommandText = sql;
                        command.Parameters.Add("p_windowStart", OracleDbType.TimeStamp, request.Window.WindowStartLocal, ParameterDirection.Input);
                        command.Parameters.Add("p_windowEnd", OracleDbType.TimeStamp, request.Window.WindowEndLocal, ParameterDirection.Input);
                        EnsureReadOnlyCommand(command);

                        var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        await using var reader = await command.ExecuteReaderAsync(token);
                        while (await reader.ReadAsync(token)) {
                            var row = new Dictionary<string, object?>(request.UniqueKeys.Count, StringComparer.OrdinalIgnoreCase);
                            for (var index = 0; index < reader.FieldCount; index++) {
                                row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                            }

                            var key = SyncBusinessKeyBuilder.Build(row, request.UniqueKeys);
                            if (!string.IsNullOrWhiteSpace(key)) {
                                keySet.Add(key);
                            }
                        }

                        return (IReadOnlySet<string>)keySet;
                    }
                    catch (OracleException oracleException) when (IsNonRetryableOracleException(oracleException)) {
                        throw new NonRetryableDangerZoneException(
                            $"Oracle 业务键读取出现不可重试错误（TableCode={request.TableCode}, SourceTable={request.SourceTable}, ErrorCode=ORA-{oracleException.Number}）。请核对 Oracle ServiceName/SID 与监听注册状态。",
                            oracleException);
                    }
                },
                ct);
        }
        catch (Exception exception) {
            logger.LogError(
                exception,
                "Oracle 业务键读取失败（含重试耗尽）。TableCode={TableCode}, SourceSchema={SourceSchema}, SourceTable={SourceTable}",
                request.TableCode,
                ResolveSourceSchemaForLog(request.SourceSchema),
                request.SourceTable);
            throw;
        }
    }

    /// <summary>
    /// 执行 ReadRowsByBusinessKeysAsync 方法。
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadRowsByBusinessKeysAsync(
        OracleBusinessKeyRowReadRequest request,
        CancellationToken ct)
    {
        // 步骤：执行 ReadRowsByBusinessKeysAsync 方法的核心处理流程。
        var sourceSchema = ResolveSourceSchema(request.SourceSchema);
        var readPlan = ValidateRowReadRequest(request, sourceSchema);
        if (readPlan.BusinessKeys.Count == 0)
        {
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        try
        {
            return await dangerZoneExecutor.ExecuteAsync(
                $"OracleRowReadByBusinessKey:{request.TableCode}",
                async token =>
                {
                    try
                    {
                        var rows = new List<IReadOnlyDictionary<string, object?>>();
                        await using var connection = new OracleConnection(_effectiveConnectionString);
                        await connection.OpenAsync(token);
                        for (var offset = 0; offset < readPlan.BusinessKeys.Count; offset += MaxBusinessKeyBatchSize)
                        {
                            var batchKeys = readPlan.BusinessKeys
                                .Skip(offset)
                                .Take(MaxBusinessKeyBatchSize)
                                .ToList();
                            if (batchKeys.Count == 0)
                            {
                                continue;
                            }

                            var parameterNames = batchKeys
                                .Select((_, index) => $"p_key{index}")
                                .ToList();
                            var sql = BuildReadRowsByBusinessKeysSql(
                                sourceSchema,
                                request.SourceTable,
                                readPlan.Columns,
                                request.BusinessKeyColumn,
                                parameterNames);

                            await using var command = connection.CreateCommand();
                            command.BindByName = true;
                            command.CommandTimeout = ResolveCommandTimeout();
                            command.CommandText = sql;
                            for (var index = 0; index < batchKeys.Count; index++)
                            {
                                command.Parameters.Add(parameterNames[index], OracleDbType.Varchar2, batchKeys[index], ParameterDirection.Input);
                            }

                            EnsureReadOnlyCommand(command);
                            await using var reader = await command.ExecuteReaderAsync(token);
                            while (await reader.ReadAsync(token))
                            {
                                var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                                for (var fieldIndex = 0; fieldIndex < reader.FieldCount; fieldIndex++)
                                {
                                    row[reader.GetName(fieldIndex)] = reader.IsDBNull(fieldIndex) ? null : reader.GetValue(fieldIndex);
                                }

                                rows.Add(row);
                            }
                        }

                        return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)rows;
                    }
                    catch (OracleException oracleException) when (IsNonRetryableOracleException(oracleException))
                    {
                        throw new NonRetryableDangerZoneException(
                            $"Oracle row read by business key hit a non-retryable error (TableCode={request.TableCode}, SourceTable={request.SourceTable}, ErrorCode=ORA-{oracleException.Number}).",
                            oracleException);
                    }
                },
                ct);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Oracle row read by business key failed after retries. TableCode={TableCode}, SourceSchema={SourceSchema}, SourceTable={SourceTable}",
                request.TableCode,
                ResolveSourceSchemaForLog(request.SourceSchema),
                request.SourceTable);
            throw;
        }
    }

    /// <summary>
    /// 执行 BuildReadPageSql 方法。
    /// </summary>
    private static string BuildReadPageSql(SyncReadRequest request, string sourceSchema) {
        // 步骤：执行 BuildReadPageSql 方法的核心处理流程。
        var orderColumns = new List<string> { $"t.{request.CursorColumn}" };
        foreach (var key in request.UniqueKeys) {
            if (!string.IsNullOrWhiteSpace(key)) {
                orderColumns.Add($"t.{key}");
            }
        }

        if (orderColumns.Count == 1) {
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
    /// 执行 BuildReadKeysSql 方法。
    /// </summary>
    private static string BuildReadKeysSql(SyncKeyReadRequest request, string sourceSchema) {
        // 步骤：执行 BuildReadKeysSql 方法的核心处理流程。
        var validUniqueKeys = request.UniqueKeys.Where(key => !string.IsNullOrWhiteSpace(key)).ToList();
        if (validUniqueKeys.Count == 0) {
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
    /// 执行 BuildReadRowsByBusinessKeysSql 方法。
    /// </summary>
    private static string BuildReadRowsByBusinessKeysSql(
        string sourceSchema,
        string sourceTable,
        IReadOnlyList<string> columns,
        string businessKeyColumn,
        IReadOnlyList<string> parameterNames)
    {
        // 步骤：执行 BuildReadRowsByBusinessKeysSql 方法的核心处理流程。
        var selectColumns = string.Join(", ", columns.Select(column => $"t.{column}"));
        var parameterList = string.Join(", ", parameterNames.Select(name => $":{name}"));
        return $"""
                SELECT {selectColumns}
                FROM {sourceSchema}.{sourceTable} t
                WHERE t.{businessKeyColumn} IN ({parameterList})
                """;
    }

    /// <summary>
    /// 执行 ValidateReadRequest 方法。
    /// </summary>
    private void ValidateReadRequest(SyncReadRequest request, string sourceSchema) {
        // 步骤：执行 ValidateReadRequest 方法的核心处理流程。
        ValidateConnectionOptions();
        EnsureReadOnlyRequest(sourceSchema, request.SourceTable, request.CursorColumn, request.UniqueKeys);
        if (request.PageNo <= 0) {
            throw new ArgumentOutOfRangeException(nameof(request.PageNo), request.PageNo, "PageNo 必须大于 0。");
        }

        if (request.PageSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(request.PageSize), request.PageSize, "PageSize 必须大于 0。");
        }
    }

    /// <summary>
    /// 执行 ValidateReadKeyRequest 方法。
    /// </summary>
    private void ValidateReadKeyRequest(SyncKeyReadRequest request, string sourceSchema) {
        // 步骤：执行 ValidateReadKeyRequest 方法的核心处理流程。
        ValidateConnectionOptions();
        EnsureReadOnlyRequest(sourceSchema, request.SourceTable, request.CursorColumn, request.UniqueKeys);
    }

    private OracleRowReadPlan ValidateRowReadRequest(OracleBusinessKeyRowReadRequest request, string sourceSchema)
    {
        ValidateConnectionOptions();
        ValidateSafeIdentifier(sourceSchema, nameof(sourceSchema));
        ValidateSafeIdentifier(request.SourceTable, nameof(request.SourceTable));
        ValidateSafeIdentifier(request.BusinessKeyColumn, nameof(request.BusinessKeyColumn));

        var columns = request.RequestedColumns
            .Where(column => !string.IsNullOrWhiteSpace(column))
            .Select(column => column.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (columns.Count == 0)
        {
            throw new InvalidOperationException("RequestedColumns cannot be empty.");
        }

        if (!columns.Contains(request.BusinessKeyColumn, StringComparer.OrdinalIgnoreCase))
        {
            columns.Insert(0, request.BusinessKeyColumn);
        }

        foreach (var column in columns)
        {
            ValidateSafeIdentifier(column, nameof(request.RequestedColumns));
        }

        var businessKeys = request.BusinessKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new OracleRowReadPlan(columns, businessKeys);
    }

    /// <summary>
    /// 执行 ResolveSourceSchema 方法。
    /// </summary>
    private string ResolveSourceSchema(string sourceSchema) {
        // 步骤：执行 ResolveSourceSchema 方法的核心处理流程。
        if (!string.IsNullOrWhiteSpace(sourceSchema)) {
            return sourceSchema;
        }

        throw new InvalidOperationException("SourceSchema 不能为空，请在 SyncJob.Tables 中为当前表显式配置 SourceSchema。");
    }

    /// <summary>
    /// 执行 ValidateConnectionOptions 方法。
    /// </summary>
    private void ValidateConnectionOptions() {
        // 步骤：执行 ValidateConnectionOptions 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(_effectiveConnectionString)) {
            throw new InvalidOperationException("Oracle.ConnectionString 不能为空。");
        }
    }

    /// <summary>
    /// 执行 BuildConnectionString 方法。
    /// </summary>
    private static string BuildConnectionString(OracleOptions options) {
        // 步骤：执行 BuildConnectionString 方法的核心处理流程。
        return OracleConnectionStringResolver.BuildEffectiveConnectionString(options);
    }

    /// <summary>
    /// 执行 ResolvePageSize 方法。
    /// </summary>
    private int ResolvePageSize(int requestedPageSize) {
        // 步骤：执行 ResolvePageSize 方法的核心处理流程。
        var maxPageSize = _options.MaxPageSize > 0 ? _options.MaxPageSize : DefaultMaxPageSize;
        if (requestedPageSize <= maxPageSize) {
            return requestedPageSize;
        }

        logger.LogWarning(
            "分页大小超过上限，已自动截断。RequestedPageSize={RequestedPageSize}, MaxPageSize={MaxPageSize}",
            requestedPageSize,
            maxPageSize);
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
    /// 执行 IsNonRetryableOracleException 方法。
    /// </summary>
    private static bool IsNonRetryableOracleException(OracleException exception) {
        // 步骤：执行 IsNonRetryableOracleException 方法的核心处理流程。
        return exception.Number is 12154 or 12514;
    }

    /// <summary>
    /// 执行 ResolveSourceSchemaForLog 方法。
    /// </summary>
    private string ResolveSourceSchemaForLog(string sourceSchema) {
        // 步骤：执行 ResolveSourceSchemaForLog 方法的核心处理流程。
        if (!string.IsNullOrWhiteSpace(sourceSchema)) {
            return sourceSchema;
        }

        return "未配置";
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
            throw new InvalidOperationException("Oracle 读取器仅允许执行 SELECT 语句。");
        }
    }

    /// <summary>
    /// 执行 EnsureReadOnlyRequest 方法。
    /// </summary>
    private static void EnsureReadOnlyRequest(
        string sourceSchema,
        string sourceTable,
        string cursorColumn,
        IReadOnlyList<string> uniqueKeys) {
            // 步骤：执行 EnsureReadOnlyRequest 方法的核心处理流程。
        ValidateSafeIdentifier(sourceSchema, nameof(sourceSchema));
        ValidateSafeIdentifier(sourceTable, nameof(sourceTable));
        ValidateSafeIdentifier(cursorColumn, nameof(cursorColumn));
        for (var index = 0; index < uniqueKeys.Count; index++) {
            var uniqueKey = uniqueKeys[index];
            if (string.IsNullOrWhiteSpace(uniqueKey)) {
                continue;
            }

            ValidateSafeIdentifier(uniqueKey, $"uniqueKeys[{index}]");
        }
    }

    /// <summary>
    /// 执行 ValidateSafeIdentifier 方法。
    /// </summary>
    private static void ValidateSafeIdentifier(string identifier, string fieldName) {
        // 步骤：执行 ValidateSafeIdentifier 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(identifier)) {
            throw new InvalidOperationException($"{fieldName} 不能为空。");
        }

        if (!identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_')) {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。");
        }
    }

    /// <summary>
    /// 定义 OracleRowReadPlan 类型。
    /// </summary>
    private readonly record struct OracleRowReadPlan(
        IReadOnlyList<string> Columns,
        IReadOnlyList<string> BusinessKeys);
}


