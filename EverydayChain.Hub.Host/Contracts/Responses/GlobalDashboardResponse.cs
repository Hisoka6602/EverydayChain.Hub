namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class GlobalDashboardResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal TotalSortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal FullCaseSortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal SplitSortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal RecognitionRatePercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal TotalVolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal TotalWeightGram { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? LatestSyncTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal DataDownloadProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal DataWritebackProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<WaveDashboardSummaryResponse> WaveSummaries { get; set; } = [];
}

