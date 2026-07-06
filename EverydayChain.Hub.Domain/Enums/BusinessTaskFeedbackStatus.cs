using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义当前类型。
/// </summary>
public enum BusinessTaskFeedbackStatus
{
    [Description("NotRequired")]
    NotRequired = 0,

    [Description("Pending")]
    Pending = 1,

    [Description("Completed")]
    Completed = 2,

    [Description("Failed")]
    Failed = 3,

    [Description("Processing")]
    Processing = 4,
}

