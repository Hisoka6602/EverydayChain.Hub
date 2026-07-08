using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Recirculation;

/// <summary>
/// 定义 RecirculationDecisionResult 类型。
/// </summary>
public sealed class RecirculationDecisionResult
{
    /// <summary>
    /// 获取或设置 ShouldRecirculate。
    /// </summary>
    public bool ShouldRecirculate { get; init; }

    /// <summary>
    /// 获取或设置 Reason。
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 获取或设置 ScanRetryCount。
    /// </summary>
    public int ScanRetryCount { get; init; }

    /// <summary>
    /// 获取或设置 RecommendedStatus。
    /// </summary>
    public BusinessTaskStatus RecommendedStatus { get; init; } = BusinessTaskStatus.Exception;
}

