namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示单个波次的汇总结果。
/// </summary>
public sealed class WaveSummaryResponse
{
    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 表示统计范围内的总数量。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 表示尚未完成分拣的数量。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 表示当前维度的分拣进度百分比。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 表示回流数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 表示异常件数量。
    /// </summary>
    public int ExceptionCount { get; set; }
}

