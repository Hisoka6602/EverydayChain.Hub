using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 BusinessTaskStatus 类型。
/// </summary>
public enum BusinessTaskStatus
{
    /// <summary>
    /// 表示业务任务已创建但尚未扫描。
    /// </summary>
    [Description("已创建")]
    Created = 1,

    /// <summary>
    /// 表示业务任务已完成扫描。
    /// </summary>
    [Description("已扫描")]
    Scanned = 2,

    /// <summary>
    /// 表示业务任务已完成落格。
    /// </summary>
    [Description("已落格")]
    Dropped = 3,

    /// <summary>
    /// 表示业务任务等待向上游系统回传。
    /// </summary>
    [Description("待回传")]
    FeedbackPending = 4,

    /// <summary>
    /// 表示业务任务进入异常状态。
    /// </summary>
    [Description("异常")]
    Exception = 5,
}

