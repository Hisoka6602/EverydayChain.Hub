namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 异常规则配置，从 <c>appsettings.json</c> 的 <c>ExceptionRule</c> 节点绑定。
/// 包括波次清理、多标签决策与回流规则的全局开关与参数。
/// </summary>
public class ExceptionRuleOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "ExceptionRule";

    /// <summary>
    /// 是否启用异常规则处理（可填写项：true、false；默认 false 表示异常规则不生效）。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 是否启用 dry-run 模式（可填写项：true、false；启用时仅评估规则并输出审计日志，不执行状态变更与持久化）。
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// 波次清理规则配置。
    /// </summary>
    public WaveCleanupRuleOptions WaveCleanup { get; set; } = new();

    /// <summary>
    /// 多标签决策规则配置。
    /// </summary>
    public MultiLabelRuleOptions MultiLabel { get; set; } = new();

    /// <summary>
    /// 回流规则配置。
    /// </summary>
    public RecirculationRuleOptions Recirculation { get; set; } = new();
}
