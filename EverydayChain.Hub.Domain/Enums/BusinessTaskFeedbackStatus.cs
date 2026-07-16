using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 BusinessTaskFeedbackStatus 类型。
/// </summary>
public enum BusinessTaskFeedbackStatus
{
    /// <summary>
    /// 表示业务任务不需要回传。
    /// </summary>
    [Description("无需回传")]
    NotRequired = 0,

    /// <summary>
    /// 表示业务任务等待回传。
    /// </summary>
    [Description("待回传")]
    Pending = 1,

    /// <summary>
    /// 表示业务任务已完成回传。
    /// </summary>
    [Description("已回传")]
    Completed = 2,

    /// <summary>
    /// 表示业务任务回传失败。
    /// </summary>
    [Description("回传失败")]
    Failed = 3,

    /// <summary>
    /// 表示业务任务正在回传。
    /// </summary>
    [Description("回传中")]
    Processing = 4,
}

