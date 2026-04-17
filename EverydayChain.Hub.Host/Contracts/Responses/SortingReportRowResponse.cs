namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 分拣报表行响应。
/// </summary>
public sealed class SortingReportRowResponse
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
    /// 回流数。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常数（仅 7 号码头显示）。
    /// </summary>
    public int ExceptionCount { get; set; }
}
