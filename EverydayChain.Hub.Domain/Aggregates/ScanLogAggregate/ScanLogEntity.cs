using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;

/// <summary>
/// 定义 ScanLogEntity 类型。
/// </summary>
public class ScanLogEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置 Id。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置 BusinessTaskId。
    /// </summary>
    public long? BusinessTaskId { get; set; }

    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    [MaxLength(64)]
    public string? TaskCode { get; set; }

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 DeviceCode。
    /// </summary>
    [MaxLength(64)]
    public string? DeviceCode { get; set; }

    /// <summary>
    /// 获取或设置 IsMatched。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 获取或设置 TraceId。
    /// </summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>
    /// 获取或设置 ScanTimeLocal。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 CreatedTimeLocal。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}

