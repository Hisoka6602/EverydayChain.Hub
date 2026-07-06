namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskWaveAggregateRow
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

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
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

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
    public int RecognitionCount { get; set; }

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
    public DateTime EarliestCreatedTimeLocal { get; set; }
}

