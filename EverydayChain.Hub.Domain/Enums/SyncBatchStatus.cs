using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 同步批次状态。
/// </summary>
public enum SyncBatchStatus
{
    /// <summary>
    /// 已创建待执行。
    /// </summary>
    [Description("待执行")]
    Pending = 1,

    /// <summary>
    /// 执行中。
    /// </summary>
    [Description("执行中")]
    InProgress = 2,

    /// <summary>
    /// 执行成功完成。
    /// </summary>
    [Description("已完成")]
    Completed = 3,

    /// <summary>
    /// 执行失败。
    /// </summary>
    [Description("执行失败")]
    Failed = 4,
}
