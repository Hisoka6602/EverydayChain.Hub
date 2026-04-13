using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;

/// <summary>
/// 扫描日志实体，记录每次扫描上传操作的完整审计轨迹，匹配成功与失败均需落盘。
/// </summary>
public class ScanLogEntity : IEntity<long>
{
    /// <summary>
    /// 主键标识。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 关联的业务任务主键；未匹配到任务时为 null。
    /// </summary>
    public long? BusinessTaskId { get; set; }

    /// <summary>
    /// 关联的业务任务编码；未匹配到任务时为 null，最大 64 字符。
    /// </summary>
    [MaxLength(64)]
    public string? TaskCode { get; set; }

    /// <summary>
    /// 扫描的条码文本，最大 128 字符。
    /// </summary>
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 扫描设备编码，最大 64 字符；不明时为 null。
    /// </summary>
    [MaxLength(64)]
    public string? DeviceCode { get; set; }

    /// <summary>
    /// 是否匹配成功。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 失败原因文本；匹配成功时为 null，最大 256 字符。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 链路追踪标识，最大 64 字符；可为 null。
    /// </summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>
    /// 扫描时间（本地时间）。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 日志记录时间（本地时间）。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}
