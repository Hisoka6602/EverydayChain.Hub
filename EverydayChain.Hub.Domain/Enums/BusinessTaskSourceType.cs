using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义当前类型。
/// </summary>
public enum BusinessTaskSourceType
{
    [Description("未知来源")]
    Unknown = 0,

    [Description("拆零")]
    Split = 1,

    [Description("整件")]
    FullCase = 2
}

