using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

/// <summary>
/// 本地统一业务任务实体，承载扫描、格口与落格链路的主状态。
/// </summary>
public class BusinessTaskEntity : IEntity<long>
{
    /// <summary>
    /// 主键标识。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 业务任务编码（来自上游任务主键或业务号），最大 64 字符。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 来源同步表编码，最大 64 字符。
    /// </summary>
    [MaxLength(64)]
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 业务键文本（由唯一键拼接得到），最大 256 字符。
    /// </summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本，最大 128 字符；尚未关联条码时可为空。
    /// </summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    /// <summary>
    /// 目标格口编码，由格口规则计算得出，最大 64 字符；未分配格口时可为空。
    /// </summary>
    [MaxLength(64)]
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 实际落格编码，落格回传时写入，最大 64 字符；尚未落格时可为空。
    /// </summary>
    [MaxLength(64)]
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 扫描设备编码，最大 64 字符；尚未扫描时可为空。
    /// </summary>
    [MaxLength(64)]
    public string? DeviceCode { get; set; }

    /// <summary>
    /// 链路追踪标识，最大 64 字符；可为空。
    /// </summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>
    /// 失败原因文本，最大 256 字符；正常状态下为空。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 任务当前状态。
    /// </summary>
    public BusinessTaskStatus Status { get; set; } = BusinessTaskStatus.Created;

    /// <summary>
    /// 业务回传状态，标识回传进度。
    /// </summary>
    public BusinessTaskFeedbackStatus FeedbackStatus { get; set; } = BusinessTaskFeedbackStatus.NotRequired;

    /// <summary>
    /// 扫描时间（本地时间）；尚未扫描时为空。
    /// </summary>
    public DateTime? ScannedAtLocal { get; set; }

    /// <summary>
    /// 落格时间（本地时间）；尚未落格时为空。
    /// </summary>
    public DateTime? DroppedAtLocal { get; set; }

    /// <summary>
    /// 创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

    /// <summary>
    /// 更新时间（本地时间）。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }
}
