namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 分拣报表响应。
/// </summary>
public sealed class SortingReportResponse
{
    /// <summary>
    /// 实际生效查询开始时间（本地时间，包含边界）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 实际生效查询结束时间（本地时间，不包含边界）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 当前生效码头筛选值。
    /// null 或空字符串表示未按码头过滤。
    /// </summary>
    public string? SelectedDockCode { get; set; }

    /// <summary>
    /// 报表行集合。
    /// 为空表示统计窗口内无可展示报表数据。
    /// </summary>
    public IReadOnlyList<SortingReportRowResponse> Rows { get; set; } = [];
}
