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

/// <summary>
/// 波次清理规则配置。
/// </summary>
public class WaveCleanupRuleOptions
{
    /// <summary>
    /// 是否启用波次清理规则（可填写项：true、false）。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 清理时将任务推进到的目标状态（可填写项：Exception；默认 Exception，表示将未完成任务标记为异常）。
    /// </summary>
    public string TargetStatusOnCleanup { get; set; } = "Exception";
}

/// <summary>
/// 多标签决策规则配置。
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

/// <summary>
/// 回流规则配置。
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
