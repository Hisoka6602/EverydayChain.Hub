using System.Data;
using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
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

        EnsureSafeIdentifier(_options.Schema, nameof(_options.Schema));
        EnsureSafeIdentifier(_options.Table, nameof(_options.Table));
        EnsureSafeIdentifier(_options.BusinessKeyColumn, nameof(_options.BusinessKeyColumn));
        EnsureSafeIdentifier(_options.FeedbackStatusColumn, nameof(_options.FeedbackStatusColumn));

        if (!string.IsNullOrWhiteSpace(_options.FeedbackTimeColumn))
        {
            EnsureSafeIdentifier(_options.FeedbackTimeColumn, nameof(_options.FeedbackTimeColumn));
        }

        if (!string.IsNullOrWhiteSpace(_options.ActualChuteColumn))
        {
            EnsureSafeIdentifier(_options.ActualChuteColumn, nameof(_options.ActualChuteColumn));
        }

        // 构建 SET 子句。
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

        var sql = $"UPDATE {_options.Schema}.{_options.Table} SET {string.Join(", ", setClauses)} WHERE {_options.BusinessKeyColumn} = :p_businessKey";

        try
        {
            return await _dangerZoneExecutor.ExecuteAsync(
                "WmsFeedbackWriteBack",
                async token =>
                {
                    await using var connection = new OracleConnection(_connectionString);
                    await connection.OpenAsync(token);
                    await using var command = connection.CreateCommand();
                    command.BindByName = true;
                    command.ArrayBindCount = tasks.Count;
                    command.CommandTimeout = _options.CommandTimeoutSeconds > 0
                        ? _options.CommandTimeoutSeconds
                        : DefaultCommandTimeoutSeconds;
                    command.CommandText = sql;

                    var feedbackStatusValues = FillArray(_options.FeedbackCompletedValue, tasks.Count);
                    command.Parameters.Add("p_feedbackStatus", OracleDbType.Varchar2, feedbackStatusValues, ParameterDirection.Input);

                    if (!string.IsNullOrWhiteSpace(_options.FeedbackTimeColumn))
                    {
                        // 统一使用批次启动时刻作为回传时间戳，保证批内时间一致性。
                        var feedbackTime = DateTime.Now;
                        command.Parameters.Add("p_feedbackTime", OracleDbType.TimeStamp, FillArray(feedbackTime, tasks.Count), ParameterDirection.Input);
                    }

                    if (!string.IsNullOrWhiteSpace(_options.ActualChuteColumn))
                    {
                        var chuteValues = tasks.Select(t => t.ActualChuteCode ?? string.Empty).ToArray();
                        command.Parameters.Add("p_actualChute", OracleDbType.Varchar2, chuteValues, ParameterDirection.Input);
                    }

                    var businessKeyValues = tasks.Select(t => t.BusinessKey).ToArray();
                    command.Parameters.Add("p_businessKey", OracleDbType.Varchar2, businessKeyValues, ParameterDirection.Input);

                    var affectedRows = await command.ExecuteNonQueryAsync(token);
                    _logger.LogInformation(
                        "WMS 回传网关：Oracle 批量写入完成。Schema={Schema}, Table={Table}, RequestedCount={RequestedCount}, AffectedRows={AffectedRows}",
                        _options.Schema, _options.Table, tasks.Count, affectedRows);
                    return affectedRows;
                },
                ct);
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
}
