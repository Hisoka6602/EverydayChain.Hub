using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.MultiLabel;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class MultiLabelDecisionResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsMultiLabel { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsDecisionMade { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? SelectedTaskCode { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<string> DiscardedTaskCodes { get; init; } = [];

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskStatus RecommendedStatus { get; init; } = BusinessTaskStatus.Exception;
}

