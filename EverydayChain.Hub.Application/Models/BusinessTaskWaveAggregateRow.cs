namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskWaveAggregateRow 类型。
/// </summary>
public sealed class BusinessTaskWaveAggregateRow
{
    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 TotalCount。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置 UnsortedCount。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 CleanableCount。
    /// </summary>
    public int CleanableCount { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseTotalCount。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseUnsortedCount。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 SplitTotalCount。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 获取或设置 SplitUnsortedCount。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 RecognitionCount。
    /// </summary>
    public int RecognitionCount { get; set; }

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
    /// 获取或设置 EarliestCreatedTimeLocal。
    /// </summary>
    public DateTime EarliestCreatedTimeLocal { get; set; }
}

