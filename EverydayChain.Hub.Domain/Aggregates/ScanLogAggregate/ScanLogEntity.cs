using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public class ScanLogEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public long? BusinessTaskId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? TaskCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? DeviceCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}

