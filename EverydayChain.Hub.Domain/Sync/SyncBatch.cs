using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 同步批次元数据。
/// </summary>
public class SyncBatch
{
    /// <summary>批次编号。</summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>父批次编号（重试关联）。</summary>
    public string? ParentBatchId { get; set; }

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

    /// <summary>批次状态。</summary>
    public SyncBatchStatus Status { get; set; } = SyncBatchStatus.Pending;

    /// <summary>开始执行时间（本地）。</summary>
    public DateTime? StartedTimeLocal { get; set; }

    /// <summary>完成执行时间（本地）。</summary>
    public DateTime? CompletedTimeLocal { get; set; }

    /// <summary>错误信息。</summary>
    public string? ErrorMessage { get; set; }
}
