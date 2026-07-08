using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 SyncChangeOperationType 类型。
/// </summary>
public enum SyncChangeOperationType
{
    [Description("新增")]
    Insert = 1,

    [Description("更新")]
    Update = 2,

    [Description("删除")]
    Delete = 3,
}

