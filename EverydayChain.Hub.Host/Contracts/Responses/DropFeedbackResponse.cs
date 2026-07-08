namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示落格回传处理结果。
/// </summary>
public sealed class DropFeedbackResponse {
    /// <summary>
    /// 表示当前请求是否已经被系统受理。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 表示业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示当前任务、波次或批次的业务状态。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

