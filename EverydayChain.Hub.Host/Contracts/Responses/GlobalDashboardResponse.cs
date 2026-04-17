namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 总看板响应。
/// </summary>
public sealed class GlobalDashboardResponse
{
    /// <summary>
    /// 统计窗口内总件数（整件 + 拆零）。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 统计窗口内未分拣数量。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 总体分拣进度百分比。
    /// 可填写范围：0~100。
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
    /// 整件分拣进度百分比。
    /// 可填写范围：0~100。
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
    /// 拆零分拣进度百分比。
    /// 可填写范围：0~100。
    /// </summary>
    public decimal SplitSortedProgressPercent { get; set; }

    /// <summary>
    /// 条码识别率百分比。
    /// 可填写范围：0~100。
    /// </summary>
    public decimal RecognitionRatePercent { get; set; }

    /// <summary>
    /// 回流件数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常件数量。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 统计窗口内测量总体积，单位立方毫米。
    /// </summary>
    public decimal TotalVolumeMm3 { get; set; }

    /// <summary>
    /// 统计窗口内测量总重量，单位克。
    /// </summary>
    public decimal TotalWeightGram { get; set; }

    /// <summary>
    /// 波次维度聚合结果集合。
    /// 空集合表示统计窗口内无波次数据。
    /// </summary>
    public IReadOnlyList<WaveDashboardSummaryResponse> WaveSummaries { get; set; } = [];
}
