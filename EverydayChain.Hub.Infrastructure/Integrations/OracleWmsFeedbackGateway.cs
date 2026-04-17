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
/// Oracle WMS 业务回传网关实现，按业务键更新 Oracle 目标表中的回传状态列。
/// 配置未启用（<see cref="WmsFeedbackOptions.Enabled"/> 为 false）时仅记录日志，不实际写入。
/// </summary>
public sealed class OracleWmsFeedbackGateway : IWmsOracleFeedbackGateway
{
    /// <summary>默认命令超时秒数。</summary>
    private const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>业务回传配置快照。</summary>
    private readonly WmsFeedbackOptions _options;

    /// <summary>Oracle 连接串。</summary>
    private readonly string _connectionString;

    /// <summary>危险动作执行器。</summary>
    private readonly IDangerZoneExecutor _dangerZoneExecutor;

    /// <summary>日志记录器。</summary>
    private readonly ILogger<OracleWmsFeedbackGateway> _logger;

    /// <summary>
    /// 初始化 Oracle WMS 业务回传网关。
    /// </summary>
    /// <param name="wmsFeedbackOptions">业务回传配置。</param>
    /// <param name="oracleOptions">Oracle 连接配置。</param>
    /// <param name="dangerZoneExecutor">危险动作执行器。</param>
    /// <param name="logger">日志记录器。</param>
    public OracleWmsFeedbackGateway(
        IOptions<WmsFeedbackOptions> wmsFeedbackOptions,
        IOptions<OracleOptions> oracleOptions,
        IDangerZoneExecutor dangerZoneExecutor,
        ILogger<OracleWmsFeedbackGateway> logger)
    {
        _options = wmsFeedbackOptions.Value;
        _connectionString = OracleConnectionStringResolver.BuildEffectiveConnectionString(oracleOptions.Value);
        _dangerZoneExecutor = dangerZoneExecutor;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> WriteFeedbackAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct)
    {
        if (tasks.Count == 0)
        {
            return 0;
        }

        // 若回传写入未启用，返回 0 表示跳过（任务状态应由调用方保持 Pending 不变）。
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
                        affectedRows += await ExecuteWriteBatchAsync(
                            connection,
                            entry.Key.Schema,
                            entry.Key.Table,
                            entry.Key.BusinessKeyColumn,
                            entry.Value,
                            feedbackTime,
                            token);
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
            _logger.LogError(
                ex,
                "WMS 回传网关：Oracle 批量写入异常。Schema={Schema}, Table={Table}, TaskCount={TaskCount}",
                _options.Schema, _options.Table, tasks.Count);
            throw;
        }
    }

    /// <summary>
    /// 执行单个目标表的批量写入。
    /// </summary>
    /// <param name="connection">已打开的 Oracle 连接。</param>
    /// <param name="schema">目标 Schema。</param>
    /// <param name="table">目标表名。</param>
    /// <param name="businessKeyColumn">目标业务键列名。</param>
    /// <param name="tasks">待写入任务列表。</param>
    /// <param name="feedbackTime">本批次回传时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>影响行数。</returns>
    private async Task<int> ExecuteWriteBatchAsync(
        OracleConnection connection,
        string schema,
        string table,
        string businessKeyColumn,
        IReadOnlyList<BusinessTaskEntity> tasks,
        DateTime feedbackTime,
        CancellationToken ct)
    {
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
    /// 按来源类型将任务分流为目标写入表。
    /// </summary>
    /// <param name="tasks">待写入任务集合。</param>
    /// <returns>分流后的任务字典。</returns>
    private Dictionary<(string Schema, string Table, string BusinessKeyColumn), List<BusinessTaskEntity>> GroupTasksByWriteTarget(
        IReadOnlyList<BusinessTaskEntity> tasks)
    {
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

    /// <summary>
    /// 按来源类型解析写入目标表配置。
    /// </summary>
    /// <param name="sourceType">来源类型。</param>
    /// <returns>目标 Schema、表名与业务键列名。</returns>
    private (string Schema, string Table, string BusinessKeyColumn) ResolveTargetBySourceType(BusinessTaskSourceType sourceType)
    {
        var schema = _options.Schema;
        var table = _options.Table;
        var businessKeyColumn = _options.BusinessKeyColumn;

        if (sourceType == BusinessTaskSourceType.Split)
        {
            schema = string.IsNullOrWhiteSpace(_options.SplitSchema) ? schema : _options.SplitSchema;
            table = string.IsNullOrWhiteSpace(_options.SplitTable) ? table : _options.SplitTable;
            businessKeyColumn = string.IsNullOrWhiteSpace(_options.SplitBusinessKeyColumn) ? businessKeyColumn : _options.SplitBusinessKeyColumn;
        }
        else if (sourceType == BusinessTaskSourceType.FullCase)
        {
            schema = string.IsNullOrWhiteSpace(_options.FullCaseSchema) ? schema : _options.FullCaseSchema;
            table = string.IsNullOrWhiteSpace(_options.FullCaseTable) ? table : _options.FullCaseTable;
            businessKeyColumn = string.IsNullOrWhiteSpace(_options.FullCaseBusinessKeyColumn) ? businessKeyColumn : _options.FullCaseBusinessKeyColumn;
        }

        EnsureSafeIdentifier(schema, nameof(schema));
        EnsureSafeIdentifier(table, nameof(table));
        EnsureSafeIdentifier(businessKeyColumn, nameof(businessKeyColumn));
        return (schema, table, businessKeyColumn);
    }

    /// <summary>
    /// 校验共享列配置安全性。
    /// </summary>
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

    /// <summary>
    /// 构建 Oracle 更新语句。
    /// </summary>
    /// <param name="schema">目标 Schema。</param>
    /// <param name="table">目标表。</param>
    /// <param name="businessKeyColumn">业务键列。</param>
    /// <returns>更新 SQL。</returns>
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

    /// <summary>
    /// 标识符安全校验，仅允许字母、数字与下划线。
    /// </summary>
    /// <param name="identifier">标识符文本。</param>
    /// <param name="fieldName">字段名（用于错误消息）。</param>
    private static void EnsureSafeIdentifier(string identifier, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。");
        }
    }

    /// <summary>
    /// 可选标识符安全校验；为空时跳过。
    /// </summary>
    /// <param name="identifier">标识符文本。</param>
    /// <param name="fieldName">字段名（用于错误消息）。</param>
    private static void EnsureOptionalIdentifierSafe(string? identifier, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            EnsureSafeIdentifier(identifier, fieldName);
        }
    }

    /// <summary>
    /// 创建指定长度并填充相同值的数组。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="value">填充值。</param>
    /// <param name="count">数组长度。</param>
    /// <returns>填充后的数组。</returns>
    private static T[] FillArray<T>(T value, int count)
    {
        var arr = new T[count];
        Array.Fill(arr, value);
        return arr;
    }

    /// <summary>
    /// 构建可空字符串参数数组，空值写入数据库空值。
    /// </summary>
    /// <param name="tasks">任务集合。</param>
    /// <param name="selector">值选择器。</param>
    /// <returns>可空字符串数组。</returns>
    private static object[] BuildNullableStringArray(
        IReadOnlyList<BusinessTaskEntity> tasks,
        Func<BusinessTaskEntity, string?> selector)
    {
        return tasks
            .Select(task =>
            {
                var value = selector(task);
                return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
            })
            .ToArray();
    }

    /// <summary>
    /// 构建可空时间参数数组，空值写入数据库空值。
    /// </summary>
    /// <param name="tasks">任务集合。</param>
    /// <param name="selector">值选择器。</param>
    /// <returns>可空时间数组。</returns>
    private static object[] BuildNullableDateTimeArray(
        IReadOnlyList<BusinessTaskEntity> tasks,
        Func<BusinessTaskEntity, DateTime?> selector)
    {
        return tasks
            .Select(task => selector(task) is DateTime value ? (object)value : DBNull.Value)
            .ToArray();
    }

    /// <summary>
    /// 构建可空小数参数数组，空值写入数据库空值。
    /// </summary>
    /// <param name="tasks">任务集合。</param>
    /// <param name="selector">值选择器。</param>
    /// <returns>可空小数数组。</returns>
    private static object[] BuildNullableDecimalArray(
        IReadOnlyList<BusinessTaskEntity> tasks,
        Func<BusinessTaskEntity, decimal?> selector)
    {
        return tasks
            .Select(task => selector(task) is decimal value ? (object)value : DBNull.Value)
            .ToArray();
    }
}
