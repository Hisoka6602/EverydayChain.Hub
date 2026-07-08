namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示单个同步批次的执行结果。
/// </summary>
public sealed class ManualSyncBatchResponse
{
    /// <summary>
    /// 表示同步批次标识。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 表示同步表或业务表编码。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示当前任务、波次或批次的业务状态。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 表示本次读取的记录数量。
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 表示本次新增写入的记录数量。
    /// </summary>
    public int InsertCount { get; set; }

    /// <summary>
    /// 表示本次更新写入的记录数量。
    /// </summary>
    public int UpdateCount { get; set; }

    /// <summary>
    /// 表示本次删除处理的记录数量。
    /// </summary>
    public int DeleteCount { get; set; }

    /// <summary>
    /// 表示本次因重复或无变更而跳过的记录数量。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 表示批次执行失败时的错误说明。
    /// </summary>
    public string? ErrorMessage { get; set; }
}

