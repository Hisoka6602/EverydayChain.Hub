using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;

/// <summary>
/// 定义 DropLogEntity 类型。
/// </summary>
public class DropLogEntity : IEntity<long>
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
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置 ActualChuteCode。
    /// </summary>
    [MaxLength(64)]
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 IsSuccess。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 获取或设置 DropTimeLocal。
    /// </summary>
    public DateTime? DropTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 CreatedTimeLocal。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}

