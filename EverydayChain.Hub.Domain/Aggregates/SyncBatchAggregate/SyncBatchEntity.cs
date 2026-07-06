using System.ComponentModel.DataAnnotations;
using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncBatchEntity : IEntity<long>
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
    public DateTime WindowStartLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime WindowEndLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int InsertCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int UpdateCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int DeleteCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public SyncBatchStatus Status { get; set; } = SyncBatchStatus.Pending;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? StartedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? CompletedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(1024)]
    public string? ErrorMessage { get; set; }
}

