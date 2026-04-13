namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 落格回传返回体。
/// </summary>
public sealed class DropFeedbackResponse {
    /// <summary>
    /// 是否受理成功。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务状态。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
