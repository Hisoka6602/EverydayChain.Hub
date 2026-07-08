using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 SyncBatchStatus 类型。
/// </summary>
public enum SyncBatchStatus
{
    [Description("待执行")]
    Pending = 1,

    [Description("执行中")]
    InProgress = 2,

    [Description("已完成")]
    Completed = 3,

    [Description("执行失败")]
    Failed = 4,
}

