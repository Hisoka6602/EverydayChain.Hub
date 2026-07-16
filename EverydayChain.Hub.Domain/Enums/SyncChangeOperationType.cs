using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 SyncChangeOperationType 类型。
/// </summary>
public enum SyncChangeOperationType
{
    /// <summary>
    /// 表示新增同步变更。
    /// </summary>
    [Description("新增")]
    Insert = 1,

    /// <summary>
    /// 表示更新同步变更。
    /// </summary>
    [Description("更新")]
    Update = 2,

    /// <summary>
    /// 表示删除同步变更。
    /// </summary>
    [Description("删除")]
    Delete = 3,
}

