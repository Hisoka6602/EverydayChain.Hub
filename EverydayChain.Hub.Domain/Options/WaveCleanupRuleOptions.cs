namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 波次清理规则配置，作为 <see cref="ExceptionRuleOptions.WaveCleanup"/> 子配置使用。
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
