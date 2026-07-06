using System.ComponentModel.DataAnnotations;
using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.SyncDeletionLogAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncDeletionLogEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? ParentBatchId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Executed { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? DeletedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(1024)]
    public string SourceEvidence { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}

