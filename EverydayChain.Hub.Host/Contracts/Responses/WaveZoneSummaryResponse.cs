namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示单个波次分区的统计结果。
/// </summary>
public sealed class WaveZoneSummaryResponse
{
    /// <summary>
    /// 表示波次分区编码。
    /// </summary>
    public string ZoneCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示波次分区名称。
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

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

