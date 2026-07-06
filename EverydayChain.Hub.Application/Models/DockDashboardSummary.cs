namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DockDashboardSummary
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SortedCount { get; set; }
}

