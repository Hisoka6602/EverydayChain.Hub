using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 BusinessTaskSourceType 类型。
/// </summary>
public enum BusinessTaskSourceType
{
    /// <summary>
    /// 表示业务任务来源未知。
    /// </summary>
    [Description("未知来源")]
    Unknown = 0,

    /// <summary>
    /// 表示业务任务来源为拆零链路。
    /// </summary>
    [Description("拆零")]
    Split = 1,

    /// <summary>
    /// 表示业务任务来源为整件链路。
    /// </summary>
    [Description("整件")]
    FullCase = 2
}

