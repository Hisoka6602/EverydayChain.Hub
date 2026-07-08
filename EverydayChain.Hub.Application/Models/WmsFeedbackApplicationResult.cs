namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WmsFeedbackApplicationResult 类型。
/// </summary>
public sealed class WmsFeedbackApplicationResult
{
    /// <summary>
    /// 获取或设置 PendingCount。
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// 获取或设置 SuccessCount。
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 获取或设置 FailedCount。
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 获取或设置 IsSuccess。
    /// </summary>
    public bool IsSuccess => FailedCount == 0;

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    public string? FailureReason { get; set; }
}

