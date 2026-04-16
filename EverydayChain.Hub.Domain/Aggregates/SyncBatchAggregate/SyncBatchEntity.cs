using System.ComponentModel.DataAnnotations;
using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;

/// <summary>
/// 同步批次持久化实体。
/// </summary>
public class SyncBatchEntity : IEntity<long>
{
    /// <summary>自增主键。</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>批次编号（全局唯一，最大 64 字符）。</summary>
    [MaxLength(64)]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>父批次编号（重试关联，最大 64 字符）。</summary>
    [MaxLength(64)]
    public string? ParentBatchId { get; set; }

    /// <summary>表编码（最大 64 字符）。</summary>
    [MaxLength(64)]
    public string TableCode { get; set; } = string.Empty;

    /// <summary>窗口起始本地时间。</summary>
    public DateTime WindowStartLocal { get; set; }

    /// <summary>窗口结束本地时间。</summary>
    public DateTime WindowEndLocal { get; set; }

    /// <summary>读取行数。</summary>
    public int ReadCount { get; set; }

    /// <summary>插入行数。</summary>
    public int InsertCount { get; set; }

    /// <summary>更新行数。</summary>
    public int UpdateCount { get; set; }

    /// <summary>删除行数。</summary>
    public int DeleteCount { get; set; }

    /// <summary>跳过行数。</summary>
    public int SkipCount { get; set; }

    /// <summary>批次状态。</summary>
    public SyncBatchStatus Status { get; set; } = SyncBatchStatus.Pending;

    /// <summary>开始执行时间（本地）。</summary>
    public DateTime? StartedTimeLocal { get; set; }

    /// <summary>完成执行时间（本地）。</summary>
    public DateTime? CompletedTimeLocal { get; set; }

    /// <summary>错误信息（最大 1024 字符）。</summary>
    [MaxLength(1024)]
    public string? ErrorMessage { get; set; }
}
