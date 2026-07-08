namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示总看板汇总结果。
/// </summary>
public sealed class GlobalDashboardResponse
{
    /// <summary>
    /// 表示统计范围内的总数量。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 表示尚未完成分拣的数量。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 表示整体分拣进度百分比。
    /// </summary>
    public decimal TotalSortedProgressPercent { get; set; }

    /// <summary>
    /// 表示整件总数量。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 表示整件待分拣数量。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 表示整件分拣进度百分比。
    /// </summary>
    public decimal FullCaseSortedProgressPercent { get; set; }

    /// <summary>
    /// 表示拆零总数量。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 表示拆零待分拣数量。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 表示拆零分拣进度百分比。
    /// </summary>
    public decimal SplitSortedProgressPercent { get; set; }

    /// <summary>
    /// 表示读码识别率百分比。
    /// </summary>
    public decimal RecognitionRatePercent { get; set; }

    /// <summary>
    /// 表示回流数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 表示异常件数量。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 表示累计体积，单位为立方毫米。
    /// </summary>
    public decimal TotalVolumeMm3 { get; set; }

    /// <summary>
    /// 表示累计重量，单位为克。
    /// </summary>
    public decimal TotalWeightGram { get; set; }

    /// <summary>
    /// 表示最近一次同步完成时间（本地时间）。
    /// </summary>
    public DateTime? LatestSyncTimeLocal { get; set; }

    /// <summary>
    /// 表示源数据下载进度百分比。
    /// </summary>
    public decimal DataDownloadProgressPercent { get; set; }

    /// <summary>
    /// 表示源数据回传进度百分比。
    /// </summary>
    public decimal DataWritebackProgressPercent { get; set; }

    /// <summary>
    /// 表示各波次维度的汇总结果列表。
    /// </summary>
    public IReadOnlyList<WaveDashboardSummaryResponse> WaveSummaries { get; set; } = [];
}

