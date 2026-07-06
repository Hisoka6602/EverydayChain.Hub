using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义当前类型。
/// </summary>
public enum SyncMode
{
    [Description("键控合并模式")]
    KeyedMerge = 1,

    [Description("状态驱动消费模式")]
    StatusDriven = 2,
}

