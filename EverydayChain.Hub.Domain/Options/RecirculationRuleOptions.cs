namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 回流规则配置，作为 <see cref="ExceptionRuleOptions.Recirculation"/> 子配置使用。
/// </summary>
public class RecirculationRuleOptions
{
    /// <summary>
    /// 是否启用回流规则（可填写项：true、false）。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 触发回流的最大扫描重试次数（可填写范围：1~100；超过该次数则触发回流，默认 3）。
    /// </summary>
    public int MaxScanRetries { get; set; } = 3;
}
