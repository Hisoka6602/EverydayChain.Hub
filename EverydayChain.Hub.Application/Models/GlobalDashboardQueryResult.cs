namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 GlobalDashboardQueryResult 类型。
/// </summary>
public sealed class GlobalDashboardQueryResult
{
    /// <summary>
    /// 获取或设置 TotalCount。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置 UnsortedCount。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 TotalSortedProgressPercent。
    /// </summary>
    public decimal TotalSortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseTotalCount。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseUnsortedCount。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseSortedProgressPercent。
    /// </summary>
    public decimal FullCaseSortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置 SplitTotalCount。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 获取或设置 SplitUnsortedCount。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 SplitSortedProgressPercent。
    /// </summary>
    public decimal SplitSortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置 RecognitionRatePercent。
    /// </summary>
    public decimal RecognitionRatePercent { get; set; }

    /// <summary>
    /// 获取或设置 RecirculatedCount。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置 ExceptionCount。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 获取或设置 TotalVolumeMm3。
    /// </summary>
    public decimal TotalVolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置 TotalWeightGram。
    /// </summary>
    public decimal TotalWeightGram { get; set; }

    /// <summary>
    /// 获取或设置 LatestSyncTimeLocal。
    /// </summary>
    public DateTime? LatestSyncTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 DataDownloadProgressPercent。
    /// </summary>
    public decimal DataDownloadProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置 DataWritebackProgressPercent。
    /// </summary>
    public decimal DataWritebackProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置 WaveSummaries。
    /// </summary>
    public IReadOnlyList<WaveDashboardSummary> WaveSummaries { get; set; } = [];
}

