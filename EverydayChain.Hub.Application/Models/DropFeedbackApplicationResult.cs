namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 落格回传应用层执行结果。
/// </summary>
public sealed class DropFeedbackApplicationResult {
    /// <summary>
    /// 是否处理成功。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 当前任务状态。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 结果描述。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 失败原因；成功时为空。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}
