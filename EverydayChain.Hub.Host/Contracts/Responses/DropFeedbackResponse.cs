namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 落格回传返回体。
/// </summary>
public sealed class DropFeedbackResponse {
    /// <summary>
    /// 落格回传是否受理成功。
    /// true 表示回传已写入任务状态流转；false 表示回传未被受理。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 业务任务编码。
    /// 返回本次回传关联的任务编码，用于前端追踪后续状态。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务状态文本。
    /// 常见值：Created（已创建）、Scanned（已扫描）、Dropped（已落格）、FeedbackPending（待回传）、Exception（异常）。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
