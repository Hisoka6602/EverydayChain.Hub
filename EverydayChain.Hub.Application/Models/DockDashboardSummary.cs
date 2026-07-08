namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 DockDashboardSummary 类型。
/// </summary>
public sealed class DockDashboardSummary
{
    /// <summary>
    /// 获取或设置 DockCode。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SplitUnsortedCount。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseUnsortedCount。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 RecirculatedCount。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置 ExceptionCount。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 获取或设置 SortedProgressPercent。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置 SortedCount。
    /// </summary>
    public int SortedCount { get; set; }
}

