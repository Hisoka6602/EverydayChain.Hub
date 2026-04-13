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
    /// 任务当前状态。
    /// </summary>
    public BusinessTaskStatus Status { get; set; } = BusinessTaskStatus.Created;

    /// <summary>
    /// 创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

    /// <summary>
    /// 更新时间（本地时间）。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }
}
