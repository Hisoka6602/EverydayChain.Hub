namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 分拣报表响应。
/// </summary>
public sealed class SortingReportResponse
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
    /// 当前生效码头筛选。
    /// </summary>
    public string? SelectedDockCode { get; set; }

    /// <summary>
    /// 报表行集合。
    /// </summary>
    public IReadOnlyList<SortingReportRowResponse> Rows { get; set; } = [];
}
