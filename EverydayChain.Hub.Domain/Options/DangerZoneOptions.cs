namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class DangerZoneOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "DangerZone";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int AutoMigrateTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool AllowAutoCreateDatabase { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 1;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CircuitBreakerSamplingDurationMinutes { get; set; } = 1;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 20;
}

