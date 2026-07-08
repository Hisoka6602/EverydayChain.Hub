using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义 SyncDeletionLog 类型。
/// </summary>
public class SyncDeletionLog
{
    /// <summary>
    /// 获取或设置 BatchId。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 ParentBatchId。
    /// </summary>
    public string? ParentBatchId { get; set; }

    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 BusinessKey。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 DeletionPolicy。
    /// </summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置 Executed。
    /// </summary>
    public bool Executed { get; set; }

    /// <summary>
    /// 获取或设置 DeletedTimeLocal。
    /// </summary>
    public DateTime? DeletedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 SourceEvidence。
    /// </summary>
    public string SourceEvidence { get; set; } = string.Empty;
}

