using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 落格回传请求。
/// </summary>
public sealed class DropFeedbackRequest {
    /// <summary>
    /// 业务任务编码，长度范围 0~64。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本，长度范围 1~128。
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 实际落格编码，长度范围 1~64。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ActualChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 落格时间（本地时间）。
    /// </summary>
    public DateTime DropTimeLocal { get; set; }
}
