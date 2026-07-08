namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示分拣报表中的单个码头统计结果。
/// </summary>
public sealed class SortingReportRowResponse
{
    /// <summary>
    /// 表示码头编码。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示拆零总数量。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 表示整件总数量。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 表示拆零已分拣数量。
    /// </summary>
    public int SplitSortedCount { get; set; }

    /// <summary>
    /// 表示整件已分拣数量。
    /// </summary>
    public int FullCaseSortedCount { get; set; }

    /// <summary>
    /// 表示回流数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 表示异常件数量。
    /// </summary>
    public int ExceptionCount { get; set; }
}

