namespace EverydayChain.Hub.Infrastructure.Options;

/// <summary>
/// 批量写入自动调谐配置，从 <c>appsettings.json</c> 的 <c>AutoTune</c> 节点绑定。
/// </summary>
public class AutoTuneOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "AutoTune";

    /// <summary>是否启用自动调谐，默认启用。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>初始批量大小，默认 200 条/批。</summary>
    public int InitialBatchSize { get; set; } = 200;

    /// <summary>允许调降至的最小批量大小，默认 20 条/批。</summary>
    public int MinBatchSize { get; set; } = 20;

    /// <summary>允许调升至的最大批量大小，默认 2000 条/批。</summary>
    public int MaxBatchSize { get; set; } = 2_000;

    /// <summary>每次调升的步长，默认 50。</summary>
    public int IncreaseStep { get; set; } = 50;

    /// <summary>每次调降的步长，默认 100。</summary>
    public int DecreaseStep { get; set; } = 100;

    /// <summary>触发降速的耗时阈值（毫秒），超出则判定为慢写入，默认 400ms。</summary>
    public double SlowThresholdMilliseconds { get; set; } = 400;
}
