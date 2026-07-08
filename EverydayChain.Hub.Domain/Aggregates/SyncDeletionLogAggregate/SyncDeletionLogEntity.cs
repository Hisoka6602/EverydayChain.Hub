using System.ComponentModel.DataAnnotations;
using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.SyncDeletionLogAggregate;

/// <summary>
/// 定义 SyncDeletionLogEntity 类型。
/// </summary>
public class SyncDeletionLogEntity : IEntity<long>
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
    /// 获取或设置 BusinessKey。
    /// </summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 DeletionPolicy。
    /// </summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置 Executed。
    /// </summary>
    public bool Executed { get; set; }

    /// <summary>
    /// 获取或设置 DeletedTimeLocal。
    /// </summary>
    public DateTime? DeletedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 SourceEvidence。
    /// </summary>
    [MaxLength(1024)]
    public string SourceEvidence { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 CreatedTimeLocal。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}

