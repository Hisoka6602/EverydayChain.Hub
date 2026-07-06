using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义当前类型。
/// </summary>
public enum SyncTablePriority
{
    [Description("低优先级")]
    Low = 1,

    [Description("高优先级")]
    High = 2,
}

