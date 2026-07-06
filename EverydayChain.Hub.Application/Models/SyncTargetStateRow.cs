namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed record class SyncTargetStateRow
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BusinessKey { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string RowDigest { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? CursorLocal { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsSoftDeleted { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? SoftDeletedTimeLocal { get; init; }
}

