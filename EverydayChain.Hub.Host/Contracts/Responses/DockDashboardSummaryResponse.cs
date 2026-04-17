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
    /// 异常数量（仅 7 号码头展示业务值，其他码头通常为 0）。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 分拣进度百分比。
    /// 可填写范围：0~100。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 已分拣总数（拆零已分拣 + 整件已分拣）。
    /// </summary>
    public int SortedCount { get; set; }
}
