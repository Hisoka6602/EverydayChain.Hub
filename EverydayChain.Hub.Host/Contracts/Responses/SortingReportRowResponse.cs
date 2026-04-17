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
    /// 拆零已分拣数量。
    /// </summary>
    public int SplitSortedCount { get; set; }

    /// <summary>
    /// 整件已分拣数量。
    /// </summary>
    public int FullCaseSortedCount { get; set; }

    /// <summary>
    /// 回流数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常数量（仅 7 号码头展示业务值，其他码头通常为 0）。
    /// </summary>
    public int ExceptionCount { get; set; }
}
