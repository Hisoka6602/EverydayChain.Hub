using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Recirculation;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class RecirculationDecisionResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool ShouldRecirculate { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ScanRetryCount { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskStatus RecommendedStatus { get; init; } = BusinessTaskStatus.Exception;
}

