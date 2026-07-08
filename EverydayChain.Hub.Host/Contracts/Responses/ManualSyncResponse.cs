namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示手工同步整体执行结果。
/// </summary>
public sealed class ManualSyncResponse
{
    /// <summary>
    /// 表示本次手工同步触发时间（本地时间）。
    /// </summary>
    public DateTime TriggeredAtLocal { get; set; }

    /// <summary>
    /// 表示本次同步执行产生的批次数量。
    /// </summary>
    public int TotalBatchCount { get; set; }

    /// <summary>
    /// 表示执行成功的批次数量。
    /// </summary>
    public int SuccessBatchCount { get; set; }

    /// <summary>
    /// 表示执行失败的批次数量。
    /// </summary>
    public int FailedBatchCount { get; set; }

    /// <summary>
    /// 表示当前结果包含的明细列表。
    /// </summary>
    public IReadOnlyList<ManualSyncBatchResponse> Items { get; set; } = [];
}

