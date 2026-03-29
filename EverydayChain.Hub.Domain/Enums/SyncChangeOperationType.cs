using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 同步变更操作类型。
/// </summary>
public enum SyncChangeOperationType
{
    /// <summary>
    /// 新增。
    /// </summary>
    [Description("新增")]
    Insert = 1,

    /// <summary>
    /// 更新。
    /// </summary>
    [Description("更新")]
    Update = 2,

    /// <summary>
    /// 删除。
    /// </summary>
    [Description("删除")]
    Delete = 3,
}
