using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncDeletionLog
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
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Executed { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? DeletedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceEvidence { get; set; } = string.Empty;
}

