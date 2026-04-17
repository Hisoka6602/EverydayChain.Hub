namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 码头看板查询请求。
/// </summary>
public sealed class DockDashboardQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含）。
    /// 可填写范围：为空时默认当天 00:00:00，本地时间。
    /// </summary>
    public DateTime? StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含）。
    /// 可填写范围：为空时默认次日 00:00:00，本地时间。
    /// </summary>
    public DateTime? EndTimeLocal { get; set; }

    /// <summary>
    /// 波次号筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// </summary>
    public string? WaveCode { get; set; }
}
