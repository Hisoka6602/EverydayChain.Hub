namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 码头看板响应。
/// </summary>
public sealed class DockDashboardResponse
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
    /// 波次选项集合。
    /// </summary>
    public IReadOnlyList<string> WaveOptions { get; set; } = [];

    /// <summary>
    /// 当前生效波次筛选。
    /// </summary>
    public string? SelectedWaveCode { get; set; }

    /// <summary>
    /// 码头汇总集合。
    /// </summary>
    public IReadOnlyList<DockDashboardSummaryResponse> DockSummaries { get; set; } = [];
}
