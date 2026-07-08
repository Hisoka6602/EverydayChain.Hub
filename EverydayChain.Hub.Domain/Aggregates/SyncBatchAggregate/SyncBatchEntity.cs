using System.ComponentModel.DataAnnotations;
using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;

/// <summary>
/// 定义 SyncBatchEntity 类型。
/// </summary>
public class SyncBatchEntity : IEntity<long>
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
    /// 获取或设置 WindowStartLocal。
    /// </summary>
    public DateTime WindowStartLocal { get; set; }

    /// <summary>
    /// 获取或设置 WindowEndLocal。
    /// </summary>
    public DateTime WindowEndLocal { get; set; }

    /// <summary>
    /// 获取或设置 ReadCount。
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 获取或设置 InsertCount。
    /// </summary>
    public int InsertCount { get; set; }

    /// <summary>
    /// 获取或设置 UpdateCount。
    /// </summary>
    public int UpdateCount { get; set; }

    /// <summary>
    /// 获取或设置 DeleteCount。
    /// </summary>
    public int DeleteCount { get; set; }

    /// <summary>
    /// 获取或设置 SkipCount。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置 Status。
    /// </summary>
    public SyncBatchStatus Status { get; set; } = SyncBatchStatus.Pending;

    /// <summary>
    /// 获取或设置 StartedTimeLocal。
    /// </summary>
    public DateTime? StartedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 CompletedTimeLocal。
    /// </summary>
    public DateTime? CompletedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 ErrorMessage。
    /// </summary>
    [MaxLength(1024)]
    public string? ErrorMessage { get; set; }
}

