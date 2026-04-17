namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 码头看板汇总项响应。
/// </summary>
public sealed class DockDashboardSummaryResponse
{
    /// <summary>
    /// 码头号。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 拆零未分拣数量。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 整件未分拣数量。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 回流数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常数量（仅 7 号码头显示）。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 分拣进度百分比。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 已分拣总数。
    /// </summary>
    public int SortedCount { get; set; }
}
