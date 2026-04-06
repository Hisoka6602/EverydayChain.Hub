namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 目标端轻量幂等状态行。
/// </summary>
public class SyncTargetStateRow
{
    /// <summary>业务键。</summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>行摘要。</summary>
    public string RowDigest { get; set; } = string.Empty;

    /// <summary>游标本地时间。</summary>
    public DateTime? CursorLocal { get; set; }

    /// <summary>是否软删除。</summary>
    public bool IsSoftDeleted { get; set; }

    /// <summary>软删除时间（本地）。</summary>
    public DateTime? SoftDeletedTimeLocal { get; set; }
}
