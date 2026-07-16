using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 SyncBatchStatus 类型。
/// </summary>
public enum SyncBatchStatus
{
    /// <summary>
    /// 表示同步批次等待执行。
    /// </summary>
    [Description("待执行")]
    Pending = 1,

    /// <summary>
    /// 表示同步批次正在执行。
    /// </summary>
    [Description("执行中")]
    InProgress = 2,

    /// <summary>
    /// 表示同步批次已完成。
    /// </summary>
    [Description("已完成")]
    Completed = 3,

    /// <summary>
    /// 表示同步批次执行失败。
    /// </summary>
    [Description("执行失败")]
    Failed = 4,
}

