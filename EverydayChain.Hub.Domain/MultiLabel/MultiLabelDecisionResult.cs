using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.MultiLabel;

/// <summary>
/// 多标签决策结果，由多标签决策服务输出，描述对多标签场景的处置结论。
/// </summary>
public sealed class MultiLabelDecisionResult
{
    /// <summary>
    /// 是否检测到多标签场景。
    /// </summary>
    public bool IsMultiLabel { get; init; }

    /// <summary>
    /// 决策是否成功（false 表示无法决策，需人工介入）。
    /// </summary>
    public bool IsDecisionMade { get; init; }

    /// <summary>
    /// 决策选用的任务编码；未决策时为 null。
    /// </summary>
    public string? SelectedTaskCode { get; init; }

    /// <summary>
    /// 被舍弃的任务编码列表；无舍弃时为空列表。
    /// </summary>
    public IReadOnlyList<string> DiscardedTaskCodes { get; init; } = [];

    /// <summary>
    /// 决策说明文本，描述选用原因或失败原因。
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 推荐的目标状态（供调用方参考，决策后各任务应推进到的状态）。
    /// </summary>
    public BusinessTaskStatus RecommendedStatus { get; init; } = BusinessTaskStatus.Exception;
}
