namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 同步检查点，用于记录成功游标和失败信息。
/// </summary>
public class SyncCheckpoint
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>最近成功游标本地时间。</summary>
    public DateTime? LastSuccessCursorLocal { get; set; }

    /// <summary>最近成功批次编号。</summary>
    public string? LastBatchId { get; set; }

    /// <summary>最近成功时间（本地）。</summary>
    public DateTime? LastSuccessTimeLocal { get; set; }

    /// <summary>最近错误信息。</summary>
    public string? LastError { get; set; }
}
