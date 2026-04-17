namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 分拣报表查询请求。
/// </summary>
public sealed class SortingReportQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含）。
    /// 可填写范围：必须大于 DateTime.MinValue。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含）。
    /// 可填写范围：必须大于开始时间。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 码头号筛选。
    /// 可填写范围：空字符串或 null 表示全部码头。
    /// </summary>
    public string? DockCode { get; set; }
}
