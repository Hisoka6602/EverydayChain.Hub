namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 波次分区统计项。
/// </summary>
public sealed class WaveZoneSummary
{
    /// <summary>
    /// 分区编码。
    /// </summary>
    public string ZoneCode { get; set; } = string.Empty;

    /// <summary>
    /// 分区名称。
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// 总件数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 未分拣件数。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 分拣进度百分比。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 回流件数。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常件数。
    /// </summary>
    public int ExceptionCount { get; set; }
}
