namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 DropFeedbackApplicationResult 类型。
/// </summary>
public sealed class DropFeedbackApplicationResult {
    /// <summary>
    /// 获取或设置 IsAccepted。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Status。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Message。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}

