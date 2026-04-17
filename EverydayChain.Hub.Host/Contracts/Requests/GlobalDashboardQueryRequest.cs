namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 总看板查询请求。
/// </summary>
public sealed class GlobalDashboardQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }
}
