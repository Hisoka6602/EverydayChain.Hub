using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 同步删除日志记录。
/// </summary>
public class SyncDeletionLog
{
    /// <summary>批次编号。</summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>父批次编号（重试关联）。</summary>
    public string? ParentBatchId { get; set; }

    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>业务键文本。</summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>删除策略。</summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>是否已实际执行删除。</summary>
    public bool Executed { get; set; }

    /// <summary>删除时间（本地）。</summary>
    public DateTime? DeletedTimeLocal { get; set; }

    /// <summary>源端缺失证据。</summary>
    public string SourceEvidence { get; set; } = string.Empty;
}
