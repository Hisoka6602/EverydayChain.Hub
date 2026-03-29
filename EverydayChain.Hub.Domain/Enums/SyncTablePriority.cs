using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 同步表调度优先级。
/// </summary>
public enum SyncTablePriority
{
    /// <summary>
    /// 低优先级。
    /// </summary>
    [Description("低优先级")]
    Low = 1,

    /// <summary>
    /// 高优先级。
    /// </summary>
    [Description("高优先级")]
    High = 2,
}
