using System.ComponentModel.DataAnnotations;
using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.SyncDeletionLogAggregate;

/// <summary>
/// 同步删除日志持久化实体。
/// </summary>
public class SyncDeletionLogEntity : IEntity<long>
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

    /// <summary>业务键文本（最大 256 字符）。</summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>删除策略。</summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>是否已实际执行删除。</summary>
    public bool Executed { get; set; }

    /// <summary>删除时间（本地）。</summary>
    public DateTime? DeletedTimeLocal { get; set; }

    /// <summary>源端缺失证据（最大 1024 字符）。</summary>
    [MaxLength(1024)]
    public string SourceEvidence { get; set; } = string.Empty;

    /// <summary>日志入库时间（本地）。</summary>
    public DateTime CreatedTimeLocal { get; set; }
}
