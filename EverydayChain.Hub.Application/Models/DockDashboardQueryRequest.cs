namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 码头看板查询请求。
/// </summary>
public sealed class DockDashboardQueryRequest
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
    /// 波次号筛选；为空表示不过滤。
    /// </summary>
    public string? WaveCode { get; set; }
}
