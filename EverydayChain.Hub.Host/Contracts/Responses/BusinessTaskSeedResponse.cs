namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 业务任务模拟补数响应。
/// </summary>
public sealed class BusinessTaskSeedResponse
{
    /// <summary>
    /// 目标物理表名。
    /// </summary>
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 请求条码总数。
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// 过滤空白条码数量。
    /// </summary>
    public int FilteredEmptyCount { get; set; }

    /// <summary>
    /// 请求内重复条码数量。
    /// </summary>
    public int DeduplicatedCount { get; set; }

    /// <summary>
    /// 清洗去重后候选条码数量。
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// 实际插入数量。
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>
    /// 目标表内已存在而跳过数量。
    /// </summary>
    public int SkippedExistingCount { get; set; }

    /// <summary>
    /// 本次实际写入的条码集合。
    /// </summary>
    public IReadOnlyList<string> InsertedBarcodes { get; set; } = [];

    /// <summary>
    /// 因目标表已存在而跳过的条码集合。
    /// </summary>
    public IReadOnlyList<string> SkippedBarcodes { get; set; } = [];
}
