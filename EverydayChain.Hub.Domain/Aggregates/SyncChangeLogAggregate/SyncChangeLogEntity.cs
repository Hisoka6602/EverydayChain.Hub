using System.ComponentModel.DataAnnotations;
using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.SyncChangeLogAggregate;

/// <summary>
/// 同步变更日志持久化实体。
/// </summary>
public class SyncChangeLogEntity : IEntity<long>
{
    /// <summary>自增主键。</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>批次编号（最大 64 字符）。</summary>
    [MaxLength(64)]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>父批次编号（重试关联，最大 64 字符）。</summary>
    [MaxLength(64)]
    public string? ParentBatchId { get; set; }

    /// <summary>表编码（最大 64 字符）。</summary>
    [MaxLength(64)]
    public string TableCode { get; set; } = string.Empty;

    /// <summary>变更操作类型。</summary>
    public SyncChangeOperationType OperationType { get; set; } = SyncChangeOperationType.Update;

    /// <summary>业务键文本（最大 256 字符）。</summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>变更前快照（JSON 文本）。</summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>变更后快照（JSON 文本）。</summary>
    public string? AfterSnapshot { get; set; }

    /// <summary>变更时间（本地）。</summary>
    public DateTime ChangedTimeLocal { get; set; }

    /// <summary>日志入库时间（本地）。</summary>
    public DateTime CreatedTimeLocal { get; set; }
}
