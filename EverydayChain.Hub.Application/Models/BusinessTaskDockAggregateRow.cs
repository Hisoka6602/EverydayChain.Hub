namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskDockAggregateRow 类型。
/// </summary>
public sealed class BusinessTaskDockAggregateRow
{
    /// <summary>
    /// 获取或设置 DockCode。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 TotalCount。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置 SortedCount。
    /// </summary>
    public int SortedCount { get; set; }

    /// <summary>
    /// 获取或设置 SplitUnsortedCount。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseUnsortedCount。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 SplitTotalCount。
    /// </summary>
    public int SplitTotalCount { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseTotalCount。
    /// </summary>
    public int FullCaseTotalCount { get; set; }

    /// <summary>
    /// 获取或设置 SplitSortedCount。
    /// </summary>
    public int SplitSortedCount { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseSortedCount。
    /// </summary>
    public int FullCaseSortedCount { get; set; }

    /// <summary>
    /// 获取或设置 RecirculatedCount。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置 ExceptionCount。
    /// </summary>
    public int ExceptionCount { get; set; }
}

