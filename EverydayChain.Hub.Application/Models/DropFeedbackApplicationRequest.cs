namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 DropFeedbackApplicationRequest 类型。
/// </summary>
public sealed class DropFeedbackApplicationRequest {
    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 ActualChuteCode。
    /// </summary>
    public string ActualChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 DropTimeLocal。
    /// </summary>
    public DateTime DropTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 IsSuccess。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    public string? FailureReason { get; set; }
}

