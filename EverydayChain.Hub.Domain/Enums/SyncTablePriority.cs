using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 SyncTablePriority 类型。
/// </summary>
public enum SyncTablePriority
{
    /// <summary>
    /// 表示低优先级同步表。
    /// </summary>
    [Description("低优先级")]
    Low = 1,

    /// <summary>
    /// 表示高优先级同步表。
    /// </summary>
    [Description("高优先级")]
    High = 2,
}

