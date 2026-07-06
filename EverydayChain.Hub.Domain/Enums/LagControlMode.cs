using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义当前类型。
/// </summary>
public enum LagControlMode
{
    [Description("固定延迟窗口")]
    FixedDelayWindow = 1,

    [Description("动态延迟窗口")]
    DynamicDelayWindow = 2,
}

