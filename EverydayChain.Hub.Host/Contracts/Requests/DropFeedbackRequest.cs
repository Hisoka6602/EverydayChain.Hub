using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 落格回传请求。
/// </summary>
public sealed class DropFeedbackRequest {
    /// <summary>
    /// 业务任务编码。
    /// 可填写范围：长度 0~64；与 <see cref="Barcode"/> 至少提供一项。
    /// 空值语义：为空时依赖 <see cref="Barcode"/> 进行任务定位。
    /// </summary>
    [MaxLength(64)]
    public string? TaskCode { get; set; }

    /// <summary>
    /// 条码文本。
    /// 可填写范围：长度 0~128；与 <see cref="TaskCode"/> 至少提供一项。
    /// 空值语义：为空时依赖 <see cref="TaskCode"/> 进行任务定位。
    /// </summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    /// <summary>
    /// 实际落格编码。
    /// 可填写范围：长度 1~64；必填。
    /// 空值语义：空字符串或仅空白字符均视为无效请求。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ActualChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 落格时间（本地时间）。
    /// 可填写范围：本地时间语义；禁止 UTC 与带时区偏移的时间值。
    /// 空值语义：该字段为值类型，未传时会触发时间合法性校验失败。
    /// </summary>
    public DateTime DropTimeLocal { get; set; }

    /// <summary>
    /// 落格是否成功。
    /// 可填写范围：true、false；必填。
    /// 空值语义：该字段为值类型，默认 false 表示落格异常。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 落格失败原因。
    /// 可填写范围：长度 0~256；可选。
    /// 空值语义：当 <see cref="IsSuccess"/> 为 true 时应为空；为 false 时建议填写明确失败原因。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }
}
