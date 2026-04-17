namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 分拣报表查询请求。
/// </summary>
public sealed class SortingReportQueryRequest
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
    /// 码头筛选；为空表示全部码头。
    /// </summary>
    public string? DockCode { get; set; }
}
