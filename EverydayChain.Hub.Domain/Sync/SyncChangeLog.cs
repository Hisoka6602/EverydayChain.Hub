using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义 SyncChangeLog 类型。
/// </summary>
public class SyncChangeLog
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
    /// 获取或设置 OperationType。
    /// </summary>
    public SyncChangeOperationType OperationType { get; set; } = SyncChangeOperationType.Update;

    /// <summary>
    /// 获取或设置 BusinessKey。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 BeforeSnapshot。
    /// </summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>
    /// 获取或设置 AfterSnapshot。
    /// </summary>
    public string? AfterSnapshot { get; set; }

    /// <summary>
    /// 获取或设置 ChangedTimeLocal。
    /// </summary>
    public DateTime ChangedTimeLocal { get; set; }
}

