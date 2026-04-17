namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 总看板查询结果。
/// </summary>
public sealed class GlobalDashboardQueryResult
{
    /// <summary>
    /// 总件数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 未分拣数量。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 总体分拣进度（百分比）。
    /// </summary>
    public decimal TotalSortedProgressPercent { get; set; }

    /// <summary>
    /// 整件总数。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 整件未分拣数量。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 整件分拣进度（百分比）。
    /// </summary>
    public decimal FullCaseSortedProgressPercent { get; set; }

    /// <summary>
    /// 拆零总数。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 拆零未分拣数量。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 拆零分拣进度（百分比）。
    /// </summary>
    public decimal SplitSortedProgressPercent { get; set; }

    /// <summary>
    /// 识别率（百分比）。
    /// </summary>
    public decimal RecognitionRatePercent { get; set; }

    /// <summary>
    /// 回流数。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常数。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 测量总体积。
    /// </summary>
    public decimal TotalVolumeMm3 { get; set; }

    /// <summary>
    /// 测量总重量。
    /// </summary>
    public decimal TotalWeightGram { get; set; }

    /// <summary>
    /// 波次维度聚合。
    /// </summary>
    public IReadOnlyList<WaveDashboardSummary> WaveSummaries { get; set; } = [];
}
