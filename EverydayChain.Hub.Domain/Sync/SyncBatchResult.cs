namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 同步批次统计结果。
/// </summary>
public class SyncBatchResult
{
    /// <summary>批次编号。</summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>窗口起始本地时间。</summary>
    public DateTime WindowStartLocal { get; set; }

    /// <summary>窗口结束本地时间。</summary>
    public DateTime WindowEndLocal { get; set; }

    /// <summary>读取行数。</summary>
    public int ReadCount { get; set; }

    /// <summary>插入行数。</summary>
    public int InsertCount { get; set; }

    /// <summary>更新行数。</summary>
    public int UpdateCount { get; set; }

    /// <summary>删除行数。</summary>
    public int DeleteCount { get; set; }

    /// <summary>跳过行数。</summary>
    public int SkipCount { get; set; }

    /// <summary>耗时。</summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>窗口滞后分钟数。</summary>
    public double LagMinutes { get; set; }

    /// <summary>窗口积压分钟数。</summary>
    public double BacklogMinutes { get; set; }

    /// <summary>吞吐（每秒处理行数）。</summary>
    public double ThroughputRowsPerSecond { get; set; }

    /// <summary>失败率（0~1）。</summary>
    public double FailureRate { get; set; }

    /// <summary>失败时的错误信息（成功时为 null）。</summary>
    public string? FailureMessage { get; set; }
}
