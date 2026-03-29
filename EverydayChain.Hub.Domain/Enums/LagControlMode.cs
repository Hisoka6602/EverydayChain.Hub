using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 滞后控制模式。
/// </summary>
public enum LagControlMode
{
    /// <summary>
    /// 固定延迟窗口。
    /// </summary>
    [Description("固定延迟窗口")]
    FixedDelayWindow = 1,

    /// <summary>
    /// 动态延迟窗口。
    /// </summary>
    [Description("动态延迟窗口")]
    DynamicDelayWindow = 2,
}
