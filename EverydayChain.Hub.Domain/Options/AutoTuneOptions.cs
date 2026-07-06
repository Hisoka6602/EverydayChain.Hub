namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class AutoTuneOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "AutoTune";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int InitialBatchSize { get; set; } = 200;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MinBatchSize { get; set; } = 20;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MaxBatchSize { get; set; } = 2_000;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int IncreaseStep { get; set; } = 50;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int DecreaseStep { get; set; } = 100;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public double SlowThresholdMilliseconds { get; set; } = 400;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SamplingWindowSize { get; set; } = 10;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public double FailureRateThreshold { get; set; } = 0.2;
}

