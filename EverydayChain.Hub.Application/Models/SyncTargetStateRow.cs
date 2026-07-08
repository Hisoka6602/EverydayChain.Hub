namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SyncTargetStateRow 类型。
/// </summary>
public sealed record class SyncTargetStateRow
{
    /// <summary>
    /// 获取或设置 BusinessKey。
    /// </summary>
    public string BusinessKey { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置 RowDigest。
    /// </summary>
    public string RowDigest { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置 CursorLocal。
    /// </summary>
    public DateTime? CursorLocal { get; init; }

    /// <summary>
    /// 获取或设置 IsSoftDeleted。
    /// </summary>
    public bool IsSoftDeleted { get; init; }

    /// <summary>
    /// 获取或设置 SoftDeletedTimeLocal。
    /// </summary>
    public DateTime? SoftDeletedTimeLocal { get; init; }
}

