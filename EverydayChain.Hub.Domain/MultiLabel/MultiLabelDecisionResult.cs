using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.MultiLabel;

/// <summary>
/// 定义 MultiLabelDecisionResult 类型。
/// </summary>
public sealed class MultiLabelDecisionResult
{
    /// <summary>
    /// 获取或设置 IsMultiLabel。
    /// </summary>
    public bool IsMultiLabel { get; init; }

    /// <summary>
    /// 获取或设置 IsDecisionMade。
    /// </summary>
    public bool IsDecisionMade { get; init; }

    /// <summary>
    /// 获取或设置 SelectedTaskCode。
    /// </summary>
    public string? SelectedTaskCode { get; init; }

    /// <summary>
    /// 获取或设置 DiscardedTaskCodes。
    /// </summary>
    public IReadOnlyList<string> DiscardedTaskCodes { get; init; } = [];

    /// <summary>
    /// 获取或设置 Reason。
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 获取或设置 RecommendedStatus。
    /// </summary>
    public BusinessTaskStatus RecommendedStatus { get; init; } = BusinessTaskStatus.Exception;
}

