namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 FeedbackCompensationResult 类型。
/// </summary>
public sealed class FeedbackCompensationResult
{
    /// <summary>
    /// 获取或设置 TargetCount。
    /// </summary>
    public int TargetCount { get; set; }

    /// <summary>
    /// 获取或设置 RetriedCount。
    /// </summary>
    public int RetriedCount { get; set; }

    /// <summary>
    /// 获取或设置 SuccessCount。
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 获取或设置 FailedCount。
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 获取或设置 SkippedCount。
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// 获取或设置 IsSuccess。
    /// </summary>
    public bool IsSuccess => FailedCount == 0;
}

