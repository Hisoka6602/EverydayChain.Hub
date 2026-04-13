using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;

/// <summary>
/// 落格日志实体，记录每次落格回传操作的完整审计轨迹，落格成功与失败均需落盘。
/// </summary>
public class DropLogEntity : IEntity<long>
{
    /// <summary>
    /// 主键标识。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 关联的业务任务主键；未定位到任务时为 null。
    /// </summary>
    public long? BusinessTaskId { get; set; }

    /// <summary>
    /// 关联的业务任务编码；未定位到任务时为 null，最大 64 字符。
    /// </summary>
    [MaxLength(64)]
    public string? TaskCode { get; set; }

    /// <summary>
    /// 条码文本；请求中含条码时填写，最大 128 字符；不明时为 null。
    /// </summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    /// <summary>
    /// 实际落格编码；落格成功时填写，最大 64 字符；失败时可为 null。
    /// </summary>
    [MaxLength(64)]
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 是否落格成功。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 失败原因文本；落格成功时为 null，最大 256 字符。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 落格时间（本地时间）；落格成功时填写。
    /// </summary>
    public DateTime? DropTimeLocal { get; set; }

    /// <summary>
    /// 日志记录时间（本地时间）。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}
