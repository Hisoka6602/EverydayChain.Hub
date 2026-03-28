namespace EverydayChain.Hub.Infrastructure.Options;

/// <summary>
/// 危险操作隔离器弹性策略配置，从 <c>appsettings.json</c> 的 <c>DangerZone</c> 节点绑定。
/// 覆盖超时、指数退避重试与熔断三层保护策略的全部可调参数。
/// </summary>
public class DangerZoneOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "DangerZone";

    /// <summary>单次操作超时时间（秒），超出则中止并抛出超时异常，默认 30 秒。</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>最大重试次数，不含首次执行，默认 2 次（共最多 3 次执行）。</summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>首次重试前的基础等待时间（秒），后续按指数退避扩大，默认 1 秒。</summary>
    public int RetryBaseDelaySeconds { get; set; } = 1;

    /// <summary>
    /// 熔断触发失败率阈值（0~1），在采样窗口内失败比例超过此值则开路，默认 0.5（50%）。
    /// </summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// 熔断评估所需最小请求吞吐量，采样窗口内请求数不足此值时不触发熔断，默认 10。
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// 熔断失败率采样窗口时长（分钟），超出窗口的历史样本自动失效，默认 1 分钟。
    /// </summary>
    public int CircuitBreakerSamplingDurationMinutes { get; set; } = 1;

    /// <summary>
    /// 熔断持续时长（秒），处于开路状态时所有请求直接失败，到期后进入半开探测，默认 20 秒。
    /// </summary>
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 20;
}
