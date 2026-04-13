using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 业务回传状态枚举，标识业务任务回传 WMS 的进度。
/// </summary>
public enum BusinessTaskFeedbackStatus
{
    /// <summary>
    /// 无需回传：任务尚未达到业务回传条件（默认值）。
    /// 可填写范围：仅系统内部赋值，不由外部直接设置。
    /// </summary>
    [Description("无需回传")]
    NotRequired = 0,

    /// <summary>
    /// 待回传：任务已满足业务回传条件，等待执行回传。
    /// 可填写范围：仅系统内部赋值。
    /// </summary>
    [Description("待回传")]
    Pending = 1,

    /// <summary>
    /// 已回传：业务回传已成功执行并收到确认。
    /// 可填写范围：仅系统内部赋值。
    /// </summary>
    [Description("已回传")]
    Completed = 2,

    /// <summary>
    /// 回传失败：业务回传执行失败，等待补偿重试。
    /// 可填写范围：仅系统内部赋值。
    /// </summary>
    [Description("回传失败")]
    Failed = 3,
}
