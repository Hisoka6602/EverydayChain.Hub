namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 码头看板响应。
/// </summary>
public sealed class DockDashboardResponse
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
    /// 波次选项集合。
    /// 为空表示当前时间窗口内无可选波次。
    /// </summary>
    public IReadOnlyList<string> WaveOptions { get; set; } = [];

    /// <summary>
    /// 当前生效波次筛选值。
    /// null 或空字符串表示未按波次过滤。
    /// </summary>
    public string? SelectedWaveCode { get; set; }

    /// <summary>
    /// 码头汇总集合。
    /// 为空表示当前时间窗口内无码头统计数据。
    /// </summary>
    public IReadOnlyList<DockDashboardSummaryResponse> DockSummaries { get; set; } = [];
}
