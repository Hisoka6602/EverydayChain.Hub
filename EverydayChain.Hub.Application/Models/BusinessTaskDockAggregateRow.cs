namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务码头聚合行。
/// </summary>
public sealed class BusinessTaskDockAggregateRow
{
    /// <summary>
    /// 码头编码。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务总数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已分拣数。
    /// </summary>
    public int SortedCount { get; set; }

    /// <summary>
    /// 拆零未分拣数。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 整件未分拣数。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 拆零总数。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 整件总数。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 拆零已分拣数。
    /// </summary>
    public int SplitSortedCount { get; set; }

    /// <summary>
    /// 整件已分拣数。
    /// </summary>
    public int FullCaseSortedCount { get; set; }

    /// <summary>
    /// 回流数。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常数（未应用 7 号码头规则前的原始计数）。
    /// </summary>
    public int ExceptionCount { get; set; }
}
