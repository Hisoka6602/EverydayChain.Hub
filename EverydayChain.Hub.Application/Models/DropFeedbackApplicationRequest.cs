namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 落格回传应用层请求模型。
/// </summary>
public sealed class DropFeedbackApplicationRequest {
    /// <summary>
    /// 业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本。
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
}
