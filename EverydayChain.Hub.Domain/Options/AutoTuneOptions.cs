namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 表示自动调谐相关配置。
/// </summary>
public class AutoTuneOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "AutoTune";

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置 InitialBatchSize。
    /// </summary>
    public int InitialBatchSize { get; set; } = 200;

    /// <summary>
    /// 获取或设置 MinBatchSize。
    /// </summary>
    public int MinBatchSize { get; set; } = 20;

    /// <summary>
    /// 获取或设置 MaxBatchSize。
    /// </summary>
    public int MaxBatchSize { get; set; } = 2_000;

    /// <summary>
    /// 获取或设置 IncreaseStep。
    /// </summary>
    public int IncreaseStep { get; set; } = 50;

    /// <summary>
    /// 获取或设置 DecreaseStep。
    /// </summary>
    public int DecreaseStep { get; set; } = 100;

    /// <summary>
    /// 获取或设置单次执行被判定为慢查询的毫秒阈值，保留三位小数。
    /// </summary>
    public decimal SlowThresholdMilliseconds { get; set; } = 400.000M;

    /// <summary>
    /// 获取或设置 SamplingWindowSize。
    /// </summary>
    public int SamplingWindowSize { get; set; } = 10;

    /// <summary>
    /// 获取或设置触发降批的失败率阈值，保留三位小数。
    /// </summary>
    public decimal FailureRateThreshold { get; set; } = 0.200M;
}

