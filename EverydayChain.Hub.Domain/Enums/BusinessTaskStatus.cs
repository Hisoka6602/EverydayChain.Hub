using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 BusinessTaskStatus 类型。
/// </summary>
public enum BusinessTaskStatus
{
    [Description("已创建")]
    Created = 1,

    [Description("已扫描")]
    Scanned = 2,

    [Description("已落格")]
    Dropped = 3,

    [Description("待回传")]
    FeedbackPending = 4,

    [Description("异常")]
    Exception = 5,
}

