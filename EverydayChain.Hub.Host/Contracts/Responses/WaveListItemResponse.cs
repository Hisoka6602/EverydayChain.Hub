namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示波次列表中的单个波次汇总记录。
/// </summary>
public sealed class WaveListItemResponse
{
    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string WaveId { get; set; } = string.Empty;

    /// <summary>
    /// 表示备注信息。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 表示该波次对应的总包裹数量。
    /// </summary>
    public int PackageTotal { get; set; }

    /// <summary>
    /// 表示尚未完成分拣的数量。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 表示拆零任务总数量。
    /// </summary>
    public int SplitTotal { get; set; }

    /// <summary>
    /// 表示整件任务总数量。
    /// </summary>
    public int FullTotal { get; set; }

    /// <summary>
    /// 表示拆零数量在当前波次中的占比百分比。
    /// </summary>
    public decimal SplitRatioPercent { get; set; }

    /// <summary>
    /// 表示整件数量在当前波次中的占比百分比。
    /// </summary>
    public decimal FullRatioPercent { get; set; }

    /// <summary>
    /// 表示回流数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 表示异常件数量。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 表示记录创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 表示当前任务、波次或批次的业务状态。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

