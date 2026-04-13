namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 落格回传应用层请求模型。
/// </summary>
public sealed class DropFeedbackApplicationRequest {
    /// <summary>
    /// 业务任务编码；与 <see cref="Barcode"/> 至少提供一项。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本；与 <see cref="TaskCode"/> 至少提供一项。
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 实际落格编码。
    /// </summary>
    public string ActualChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 落格时间（本地时间）。
    /// </summary>
    public DateTime DropTimeLocal { get; set; }

    /// <summary>
    /// 落格是否成功；true 表示成功落格，false 表示落格异常。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 落格失败原因；仅在 <see cref="IsSuccess"/> 为 false 时有意义，最大 256 字符。
    /// </summary>
    public string? FailureReason { get; set; }
}
