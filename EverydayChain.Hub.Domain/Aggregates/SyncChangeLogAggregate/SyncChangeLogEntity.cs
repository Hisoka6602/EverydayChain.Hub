using System.ComponentModel.DataAnnotations;
using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.SyncChangeLogAggregate;

/// <summary>
/// 定义 SyncChangeLogEntity 类型。
/// </summary>
public class SyncChangeLogEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置 Id。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置 BatchId。
    /// </summary>
    [MaxLength(64)]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 ParentBatchId。
    /// </summary>
    [MaxLength(64)]
    public string? ParentBatchId { get; set; }

    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    [MaxLength(64)]
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 OperationType。
    /// </summary>
    public SyncChangeOperationType OperationType { get; set; } = SyncChangeOperationType.Update;

    /// <summary>
    /// 获取或设置 BusinessKey。
    /// </summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 BeforeSnapshot。
    /// </summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>
    /// 获取或设置 AfterSnapshot。
    /// </summary>
    public string? AfterSnapshot { get; set; }

    /// <summary>
    /// 获取或设置 ChangedTimeLocal。
    /// </summary>
    public DateTime ChangedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 CreatedTimeLocal。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}

