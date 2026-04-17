using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 业务任务来源类型枚举，标识任务来自拆零链路或整件链路。
/// </summary>
public enum BusinessTaskSourceType
{
    /// <summary>
    /// 未知来源类型。
    /// 可填写范围：仅系统内部兜底赋值。
    /// </summary>
    [Description("未知来源")]
    Unknown = 0,

    /// <summary>
    /// 拆零来源类型。
    /// 可填写范围：仅系统内部赋值。
    /// </summary>
    [Description("拆零")]
    Split = 1,

    /// <summary>
    /// 整件来源类型。
    /// 可填写范围：仅系统内部赋值。
    /// </summary>
    [Description("整件")]
    FullCase = 2
}
