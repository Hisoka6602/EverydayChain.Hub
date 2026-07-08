namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SortingReportRow 类型。
/// </summary>
public sealed class SortingReportRow
{
    /// <summary>
    /// 获取或设置 DockCode。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

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

