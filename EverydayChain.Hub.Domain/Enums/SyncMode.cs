using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 SyncMode 类型。
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// 表示按业务键执行增量合并。
    /// </summary>
    [Description("键控合并模式")]
    KeyedMerge = 1,

    /// <summary>
    /// 表示按远端状态驱动消费。
    /// </summary>
    [Description("状态驱动消费模式")]
    StatusDriven = 2,
}

