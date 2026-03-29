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
}
