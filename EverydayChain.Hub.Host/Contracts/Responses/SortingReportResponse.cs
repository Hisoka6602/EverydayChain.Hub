namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示分拣报表查询结果。
/// </summary>
public sealed class SortingReportResponse
{
    /// <summary>
    /// 表示查询或统计开始时间（本地时间）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 表示查询或统计结束时间（本地时间）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 表示本次统计实际使用的码头编码。
    /// </summary>
    public string? SelectedDockCode { get; set; }

    /// <summary>
    /// 表示当前结果包含的统计行列表。
    /// </summary>
    public IReadOnlyList<SortingReportRowResponse> Rows { get; set; } = [];
}

