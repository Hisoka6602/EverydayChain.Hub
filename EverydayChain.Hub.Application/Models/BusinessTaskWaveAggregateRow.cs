namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务波次聚合行。
/// </summary>
public sealed class BusinessTaskWaveAggregateRow
{
    /// <summary>
    /// 波次编码。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 总件数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 未分拣数。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 整件总数。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 整件未分拣数。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 拆零总数。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 拆零未分拣数。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 已识别数（扫描时间非空）。
    /// </summary>
    public int RecognitionCount { get; set; }

    /// <summary>
    /// 回流数。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常数。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 总体积。
    /// </summary>
    public decimal TotalVolumeMm3 { get; set; }

    /// <summary>
    /// 总重量。
    /// </summary>
    public decimal TotalWeightGram { get; set; }
}
