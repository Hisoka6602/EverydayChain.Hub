namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 多标签决策规则配置，作为 <see cref="ExceptionRuleOptions.MultiLabel"/> 子配置使用。
/// </summary>
public class MultiLabelRuleOptions
{
    /// <summary>
    /// 是否启用多标签决策规则（可填写项：true、false）。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 发现多标签时的处理策略（可填写项：UseFirst、UseLatest、MarkException；
    /// UseFirst 取最早匹配项，UseLatest 取最新匹配项，MarkException 直接标记为异常；默认 MarkException）。
    /// </summary>
    public string Strategy { get; set; } = "MarkException";
}
