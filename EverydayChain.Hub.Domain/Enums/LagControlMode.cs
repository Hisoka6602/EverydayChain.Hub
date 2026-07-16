using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 LagControlMode 类型。
/// </summary>
public enum LagControlMode
{
    /// <summary>
    /// 表示使用固定延迟窗口控制同步水位。
    /// </summary>
    [Description("固定延迟窗口")]
    FixedDelayWindow = 1,

    /// <summary>
    /// 表示按运行状态动态调整延迟窗口。
    /// </summary>
    [Description("动态延迟窗口")]
    DynamicDelayWindow = 2,
}

