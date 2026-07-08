namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示业务任务手工补数执行结果。
/// </summary>
public sealed class BusinessTaskSeedResponse
{
    /// <summary>
    /// 表示补数写入的目标表名。
    /// </summary>
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 表示请求提交的记录数量。
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// 表示因空值被过滤掉的记录数量。
    /// </summary>
    public int FilteredEmptyCount { get; set; }

    /// <summary>
    /// 表示去重阶段剔除的重复记录数量。
    /// </summary>
    public int DeduplicatedCount { get; set; }

    /// <summary>
    /// 表示满足条件的候选记录数量。
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// 表示成功插入本地库的记录数量。
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>
    /// 表示因本地已存在而跳过的记录数量。
    /// </summary>
    public int SkippedExistingCount { get; set; }

    /// <summary>
    /// 表示本次成功补写到本地库的条码列表。
    /// </summary>
    public IReadOnlyList<string> InsertedBarcodes { get; set; } = [];

    /// <summary>
    /// 表示本次被跳过的条码列表。
    /// </summary>
    public IReadOnlyList<string> SkippedBarcodes { get; set; } = [];
}

