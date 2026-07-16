using System.Data;
using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace EverydayChain.Hub.Infrastructure.Integrations;

/// <summary>
/// 定义 OracleWmsFeedbackGateway 类型。
/// </summary>
public sealed class OracleWmsFeedbackGateway : IWmsOracleFeedbackGateway
{
    /// <summary>
    /// 存储 DefaultCommandTimeoutSeconds 字段。
    /// </summary>
    private const int DefaultCommandTimeoutSeconds = 60;
    /// <summary>
    /// 表示没有可写入目标表时用于日志输出的占位文本。
    /// </summary>
    private const string EmptyTargetsPlaceholder = "(无目标表)";

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly WmsFeedbackOptions _options;

    /// <summary>
    /// 存储 _connectionString 字段。
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// 存储 _dangerZoneExecutor 字段。
    /// </summary>
    private readonly IDangerZoneExecutor _dangerZoneExecutor;

    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<OracleWmsFeedbackGateway> _logger;

    /// <summary>
    /// 执行 OracleWmsFeedbackGateway 方法。
    /// </summary>
    public OracleWmsFeedbackGateway(
        IOptions<WmsFeedbackOptions> wmsFeedbackOptions,
        IOptions<OracleOptions> oracleOptions,
        IDangerZoneExecutor dangerZoneExecutor,
        ILogger<OracleWmsFeedbackGateway> logger)
    {
        // 步骤：执行 OracleWmsFeedbackGateway 方法的核心处理流程。
        _options = wmsFeedbackOptions.Value;
        _connectionString = OracleConnectionStringResolver.BuildEffectiveConnectionString(oracleOptions.Value);
        _dangerZoneExecutor = dangerZoneExecutor;
        _logger = logger;
    }

    public async Task<int> WriteFeedbackAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct)
    {
        if (tasks.Count == 0)
        {
            return 0;
        }

        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "WMS 回传网关：写入开关关闭，跳过 Oracle 写入。TaskCount={TaskCount}",
                tasks.Count);
            return 0;
        }

        EnsureCommonColumnsSafe();
        var groupedTasks = GroupTasksByWriteTarget(tasks);
        var feedbackTime = DateTime.Now;
        var totalAffectedRows = 0;

        try
        {
            totalAffectedRows = await _dangerZoneExecutor.ExecuteAsync(
                "WmsFeedbackWriteBack",
                async token =>
                {
                    var affectedRows = 0;
                    await using var connection = new OracleConnection(_connectionString);
                    await connection.OpenAsync(token);
                    foreach (var entry in groupedTasks)
                    {
                        try
                        {
                            affectedRows += await ExecuteWriteBatchAsync(
                                connection,
                                entry.Key.Schema,
                                entry.Key.Table,
                                entry.Key.BusinessKeyColumn,
                                entry.Value,
                                feedbackTime,
                                token);
                        }
                        catch (Exception ex) when (!token.IsCancellationRequested)
                        {
                            _logger.LogError(
                                ex,
                                "WMS 回传网关：目标表写入失败。Schema={Schema}, Table={Table}, TaskCount={TaskCount}",
                                entry.Key.Schema,
                                entry.Key.Table,
                                entry.Value.Count);
                            throw;
                        }
                    }

                    return affectedRows;
                },
                ct);

            _logger.LogInformation(
                "WMS 回传网关：Oracle 分流批量写入完成。RequestedCount={RequestedCount}, AffectedRows={AffectedRows}, TargetCount={TargetCount}",
                tasks.Count,
                totalAffectedRows,
                groupedTasks.Count);
            return totalAffectedRows;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning(
                "WMS 回传网关：调用方取消令牌触发写入终止。TaskCount={TaskCount}",
                tasks.Count);
            throw;
        }
        catch (Exception ex)
        {
            var targetSummary = BuildTargetSummary(groupedTasks.Keys);
            _logger.LogError(
                ex,
                "WMS 回传网关：Oracle 批量写入异常。Targets={Targets}, TaskCount={TaskCount}",
                targetSummary,
                tasks.Count);
            throw;
        }
    }

    /// <summary>
    /// 执行 ExecuteWriteBatchAsync 方法。
    /// </summary>
    private async Task<int> ExecuteWriteBatchAsync(
        OracleConnection connection,
        string schema,
        string table,
        string businessKeyColumn,
        IReadOnlyList<BusinessTaskEntity> tasks,
        DateTime feedbackTime,
        CancellationToken ct)
    {
        // 步骤：执行 catch 方法的核心处理流程。
        var sql = BuildUpdateSql(schema, table, businessKeyColumn);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.ArrayBindCount = tasks.Count;
        command.CommandTimeout = _options.CommandTimeoutSeconds > 0
            ? _options.CommandTimeoutSeconds
            : DefaultCommandTimeoutSeconds;
        command.CommandText = sql;

        command.Parameters.Add(
            "p_feedbackStatus",
            OracleDbType.Varchar2,
            FillArray(_options.FeedbackCompletedValue, tasks.Count),
            ParameterDirection.Input);

        if (!string.IsNullOrWhiteSpace(_options.FeedbackTimeColumn))
        {
            command.Parameters.Add(
                "p_feedbackTime",
                OracleDbType.TimeStamp,
                FillArray(feedbackTime, tasks.Count),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.ActualChuteColumn))
        {
            command.Parameters.Add(
                "p_actualChute",
                OracleDbType.Varchar2,
                BuildNullableStringArray(tasks, task => task.ActualChuteCode),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.ScanTimeColumn))
        {
            command.Parameters.Add(
                "p_scanTime",
                OracleDbType.TimeStamp,
                BuildNullableDateTimeArray(tasks, task => task.ScannedAtLocal),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.LengthColumn))
        {
            command.Parameters.Add(
                "p_length",
                OracleDbType.Decimal,
                BuildNullableDecimalArray(tasks, task => task.LengthMm),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.WidthColumn))
        {
            command.Parameters.Add(
                "p_width",
                OracleDbType.Decimal,
                BuildNullableDecimalArray(tasks, task => task.WidthMm),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.HeightColumn))
        {
            command.Parameters.Add(
                "p_height",
                OracleDbType.Decimal,
                BuildNullableDecimalArray(tasks, task => task.HeightMm),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.VolumeColumn))
        {
            command.Parameters.Add(
                "p_volume",
                OracleDbType.Decimal,
                BuildNullableDecimalArray(tasks, task => task.VolumeMm3),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.WeightColumn))
        {
            command.Parameters.Add(
                "p_weight",
                OracleDbType.Decimal,
                BuildNullableDecimalArray(tasks, task => task.WeightGram),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.ScanCountColumn))
        {
            command.Parameters.Add(
                "p_scanCount",
                OracleDbType.Int32,
                tasks.Select(task => task.ScanCount).ToArray(),
                ParameterDirection.Input);
        }

        if (!string.IsNullOrWhiteSpace(_options.BusinessStatusColumn))
        {
            command.Parameters.Add(
                "p_businessStatus",
                OracleDbType.Int32,
                tasks.Select(task => (int)task.Status).ToArray(),
                ParameterDirection.Input);
        }

        var businessKeyValues = tasks.Select(t => t.BusinessKey).ToArray();
        command.Parameters.Add("p_businessKey", OracleDbType.Varchar2, businessKeyValues, ParameterDirection.Input);
        return await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// 执行 GroupTasksByWriteTarget 方法。
    /// </summary>
    private Dictionary<(string Schema, string Table, string BusinessKeyColumn), List<BusinessTaskEntity>> GroupTasksByWriteTarget(
        IReadOnlyList<BusinessTaskEntity> tasks)
    {
        // 步骤：执行 GroupTasksByWriteTarget 方法的核心处理流程。
        var grouped = new Dictionary<(string Schema, string Table, string BusinessKeyColumn), List<BusinessTaskEntity>>();
        foreach (var task in tasks)
        {
            var target = ResolveTargetBySourceType(task.SourceType);
            if (!grouped.TryGetValue(target, out var list))
            {
                list = [];
                grouped[target] = list;
            }

            list.Add(task);
        }

        return grouped;
    }

    private (string Schema, string Table, string BusinessKeyColumn) ResolveTargetBySourceType(BusinessTaskSourceType sourceType)
    {
        if (sourceType == BusinessTaskSourceType.Split)
        {
            EnsureSafeIdentifier(_options.SplitSchema, nameof(_options.SplitSchema));
            EnsureSafeIdentifier(_options.SplitTable, nameof(_options.SplitTable));
            EnsureSafeIdentifier(_options.SplitBusinessKeyColumn, nameof(_options.SplitBusinessKeyColumn));
            return (_options.SplitSchema, _options.SplitTable, _options.SplitBusinessKeyColumn);
        }

        if (sourceType == BusinessTaskSourceType.FullCase)
        {
            EnsureSafeIdentifier(_options.FullCaseSchema, nameof(_options.FullCaseSchema));
            EnsureSafeIdentifier(_options.FullCaseTable, nameof(_options.FullCaseTable));
            EnsureSafeIdentifier(_options.FullCaseBusinessKeyColumn, nameof(_options.FullCaseBusinessKeyColumn));
            return (_options.FullCaseSchema, _options.FullCaseTable, _options.FullCaseBusinessKeyColumn);
        }

        throw new InvalidOperationException($"不支持的业务来源类型，无法确定 WMS 回写目标表。sourceType={sourceType}");
    }

    private void EnsureCommonColumnsSafe()
    {
        EnsureSafeIdentifier(_options.FeedbackStatusColumn, nameof(_options.FeedbackStatusColumn));
        EnsureOptionalIdentifierSafe(_options.FeedbackTimeColumn, nameof(_options.FeedbackTimeColumn));
        EnsureOptionalIdentifierSafe(_options.ActualChuteColumn, nameof(_options.ActualChuteColumn));
        EnsureOptionalIdentifierSafe(_options.ScanTimeColumn, nameof(_options.ScanTimeColumn));
        EnsureOptionalIdentifierSafe(_options.LengthColumn, nameof(_options.LengthColumn));
        EnsureOptionalIdentifierSafe(_options.WidthColumn, nameof(_options.WidthColumn));
        EnsureOptionalIdentifierSafe(_options.HeightColumn, nameof(_options.HeightColumn));
        EnsureOptionalIdentifierSafe(_options.VolumeColumn, nameof(_options.VolumeColumn));
        EnsureOptionalIdentifierSafe(_options.WeightColumn, nameof(_options.WeightColumn));
        EnsureOptionalIdentifierSafe(_options.ScanCountColumn, nameof(_options.ScanCountColumn));
        EnsureOptionalIdentifierSafe(_options.BusinessStatusColumn, nameof(_options.BusinessStatusColumn));
    }

    private string BuildUpdateSql(string schema, string table, string businessKeyColumn)
    {
        var setClauses = new List<string>
        {
            $"{_options.FeedbackStatusColumn} = :p_feedbackStatus"
        };

        if (!string.IsNullOrWhiteSpace(_options.FeedbackTimeColumn))
        {
            setClauses.Add($"{_options.FeedbackTimeColumn} = :p_feedbackTime");
        }

        if (!string.IsNullOrWhiteSpace(_options.ActualChuteColumn))
        {
            setClauses.Add($"{_options.ActualChuteColumn} = :p_actualChute");
        }

        if (!string.IsNullOrWhiteSpace(_options.ScanTimeColumn))
        {
            setClauses.Add($"{_options.ScanTimeColumn} = :p_scanTime");
        }

        if (!string.IsNullOrWhiteSpace(_options.LengthColumn))
        {
            setClauses.Add($"{_options.LengthColumn} = :p_length");
        }

        if (!string.IsNullOrWhiteSpace(_options.WidthColumn))
        {
            setClauses.Add($"{_options.WidthColumn} = :p_width");
        }

        if (!string.IsNullOrWhiteSpace(_options.HeightColumn))
        {
            setClauses.Add($"{_options.HeightColumn} = :p_height");
        }

        if (!string.IsNullOrWhiteSpace(_options.VolumeColumn))
        {
            setClauses.Add($"{_options.VolumeColumn} = :p_volume");
        }

        if (!string.IsNullOrWhiteSpace(_options.WeightColumn))
        {
            setClauses.Add($"{_options.WeightColumn} = :p_weight");
        }

        if (!string.IsNullOrWhiteSpace(_options.ScanCountColumn))
        {
            setClauses.Add($"{_options.ScanCountColumn} = :p_scanCount");
        }

        if (!string.IsNullOrWhiteSpace(_options.BusinessStatusColumn))
        {
            setClauses.Add($"{_options.BusinessStatusColumn} = :p_businessStatus");
        }

        return $"UPDATE {schema}.{table} SET {string.Join(", ", setClauses)} WHERE {businessKeyColumn} = :p_businessKey";
    }

    private static void EnsureSafeIdentifier(string identifier, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException($"{fieldName} 不能为空或空白。");
        }

        if (!identifier.All(IsAsciiLetterDigitOrUnderscore))
        {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许 ASCII 字母、数字、下划线。");
        }
    }

    private static void EnsureOptionalIdentifierSafe(string? identifier, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            EnsureSafeIdentifier(identifier, fieldName);
        }
    }

    private static bool IsAsciiLetterDigitOrUnderscore(char ch)
    {
        return (ch >= 'A' && ch <= 'Z')
            || (ch >= 'a' && ch <= 'z')
            || (ch >= '0' && ch <= '9')
            || ch == '_';
    }

    private static string BuildTargetSummary(IEnumerable<(string Schema, string Table, string BusinessKeyColumn)> targets)
    {
        var entries = targets
            .Select(target => $"{target.Schema}.{target.Table}({target.BusinessKeyColumn})")
            .ToList();
        return entries.Count > 0 ? string.Join(", ", entries) : EmptyTargetsPlaceholder;
    }

    private static T[] FillArray<T>(T value, int count)
    {
        var arr = new T[count];
        Array.Fill(arr, value);
        return arr;
    }

    /// <summary>
    /// 执行 BuildNullableStringArray 方法。
    /// </summary>
    private static object[] BuildNullableStringArray(
        IReadOnlyList<BusinessTaskEntity> tasks,
        Func<BusinessTaskEntity, string?> selector)
    {
        // 步骤：执行 BuildNullableStringArray 方法的核心处理流程。
        return tasks
            .Select(task =>
            {
                var value = selector(task);
                return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
            })
            .ToArray();
    }

    /// <summary>
    /// 执行 BuildNullableDateTimeArray 方法。
    /// </summary>
    private static object[] BuildNullableDateTimeArray(
        IReadOnlyList<BusinessTaskEntity> tasks,
        Func<BusinessTaskEntity, DateTime?> selector)
    {
        // 步骤：执行 BuildNullableDateTimeArray 方法的核心处理流程。
        return tasks
            .Select(task => selector(task) is DateTime value ? (object)value : DBNull.Value)
            .ToArray();
    }

    /// <summary>
    /// 执行 BuildNullableDecimalArray 方法。
    /// </summary>
    private static object[] BuildNullableDecimalArray(
        IReadOnlyList<BusinessTaskEntity> tasks,
        Func<BusinessTaskEntity, decimal?> selector)
    {
        // 步骤：执行 BuildNullableDecimalArray 方法的核心处理流程。
        return tasks
            .Select(task => selector(task) is decimal value ? (object)value : DBNull.Value)
            .ToArray();
    }
}

