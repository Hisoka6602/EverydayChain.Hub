namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示单个码头的看板汇总结果。
/// </summary>
public sealed class DockDashboardSummaryResponse
{
    /// <summary>
    /// 表示码头编码。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示拆零待分拣数量。
    /// </summary>
    public int SplitUnsortedCount { get; set; }

    /// <summary>
    /// 表示整件待分拣数量。
    /// </summary>
    public int FullCaseUnsortedCount { get; set; }

    /// <summary>
    /// 表示回流数量。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 表示异常件数量。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 表示当前维度的分拣进度百分比。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 表示已完成分拣的数量。
    /// </summary>
    public int SortedCount { get; set; }
}

