using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 业务任务状态枚举，描述任务在扫描、落格与业务回传过程中的生命周期阶段。
/// </summary>
public enum BusinessTaskStatus
{
    /// <summary>
    /// 已创建：任务已由同步数据物化为本地业务任务，但尚未发生扫描。
    /// </summary>
    [Description("已创建")]
    Created = 1,

    /// <summary>
    /// 已扫描：任务已接收扫描输入并完成基础匹配。
    /// </summary>
    [Description("已扫描")]
    Scanned = 2,

    /// <summary>
    /// 已落格：任务已收到落格回传并确认落格结果。
    /// </summary>
    [Description("已落格")]
    Dropped = 3,

    /// <summary>
    /// 待回传：任务已达到业务回传条件，等待业务回传链路处理。
    /// </summary>
    [Description("待回传")]
    FeedbackPending = 4,

    /// <summary>
    /// 异常：任务在落格或处理过程中发生异常，需人工介入或补偿。
    /// </summary>
    [Description("异常")]
    Exception = 5,
}
