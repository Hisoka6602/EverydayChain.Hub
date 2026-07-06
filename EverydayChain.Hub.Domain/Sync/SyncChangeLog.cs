using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncChangeLog
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ParentBatchId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public SyncChangeOperationType OperationType { get; set; } = SyncChangeOperationType.Update;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? AfterSnapshot { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime ChangedTimeLocal { get; set; }
}

