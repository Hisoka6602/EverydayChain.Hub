using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 落格回传请求。
/// </summary>
public sealed class DropFeedbackRequest {
    /// <summary>
    /// 业务任务编码，长度范围 0~64；与 <see cref="Barcode"/> 至少提供一项。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本，长度范围 0~128；与 <see cref="TaskCode"/> 至少提供一项。
    /// </summary>
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

    /// <summary>
    /// 落格是否成功；true 表示落格成功，false 表示落格异常。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 落格失败原因；仅在 <see cref="IsSuccess"/> 为 false 时有意义，长度范围 0~256。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }
}
