namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 分拣报表查询结果。
/// </summary>
public sealed class SortingReportQueryResult
{
    /// <summary>
    /// 查询开始时间（本地时间，包含）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 生效码头筛选。
    /// </summary>
    public string? SelectedDockCode { get; set; }

    /// <summary>
    /// 报表行集合。
    /// </summary>
    public IReadOnlyList<SortingReportRow> Rows { get; set; } = [];
}
