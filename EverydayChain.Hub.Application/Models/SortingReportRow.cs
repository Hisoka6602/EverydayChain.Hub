namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 分拣报表行。
/// </summary>
public sealed class SortingReportRow
{
    /// <summary>
    /// 码头号。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 拆零总数。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 整件总数。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 拆零分拣数。
    /// </summary>
    public int SplitSortedCount { get; set; }

    /// <summary>
    /// 整件分拣数。
    /// </summary>
    public int FullCaseSortedCount { get; set; }

    /// <summary>
    /// 回流数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常数量（仅 7 号码头显示）。
    /// </summary>
    public int ExceptionCount { get; set; }
}
