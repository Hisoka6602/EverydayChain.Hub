namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 表示危险操作隔离器配置。
/// </summary>
public class DangerZoneOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "DangerZone";

    /// <summary>
    /// 获取或设置 TimeoutSeconds。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置 AutoMigrateTimeoutSeconds。
    /// </summary>
    public int AutoMigrateTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// 获取或设置 AllowAutoCreateDatabase。
    /// </summary>
    public bool AllowAutoCreateDatabase { get; set; }

    /// <summary>
    /// 获取或设置 MaxRetryAttempts。
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>
    /// 获取或设置 RetryBaseDelaySeconds。
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 1;

    /// <summary>
    /// 获取或设置熔断器失败占比阈值，保留三位小数。
    /// </summary>
    public decimal CircuitBreakerFailureRatio { get; set; } = 0.500M;

    /// <summary>
    /// 获取或设置 CircuitBreakerMinimumThroughput。
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// 获取或设置 CircuitBreakerSamplingDurationMinutes。
    /// </summary>
    public int CircuitBreakerSamplingDurationMinutes { get; set; } = 1;

    /// <summary>
    /// 获取或设置 CircuitBreakerBreakDurationSeconds。
    /// </summary>
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 20;
}

