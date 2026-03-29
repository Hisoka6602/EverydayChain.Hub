namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 同步变更日志记录。
/// </summary>
public class SyncChangeLog
{
    /// <summary>批次编号。</summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>父批次编号（重试关联）。</summary>
    public string? ParentBatchId { get; set; }

    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>变更操作类型（Insert/Update/Delete）。</summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>业务键文本。</summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>变更前快照。</summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>变更后快照。</summary>
    public string? AfterSnapshot { get; set; }

    /// <summary>变更时间（本地）。</summary>
    public DateTime ChangedTimeLocal { get; set; }
}
