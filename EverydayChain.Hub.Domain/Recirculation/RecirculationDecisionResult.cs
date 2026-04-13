using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Recirculation;

/// <summary>
/// 回流决策结果，由回流规则服务输出，描述对回流场景的判断与处置结论。
/// </summary>
public sealed class RecirculationDecisionResult
{
    /// <summary>
    /// 是否判定需要回流。
    /// </summary>
    public bool ShouldRecirculate { get; init; }

    /// <summary>
    /// 触发回流的原因描述；不需要回流时为 null。
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 当前扫描重试次数，用于判断是否超出上限。
    /// </summary>
    public int ScanRetryCount { get; init; }

    /// <summary>
    /// 推荐的目标状态（供调用方参考，回流判定后任务应推进到的状态）。
    /// </summary>
    public BusinessTaskStatus RecommendedStatus { get; init; } = BusinessTaskStatus.Exception;
}
