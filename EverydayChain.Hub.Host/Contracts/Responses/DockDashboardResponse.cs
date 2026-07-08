namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示码头看板查询结果。
/// </summary>
public sealed class DockDashboardResponse
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
    /// 表示可供前端筛选的波次列表。
    /// </summary>
    public IReadOnlyList<string> WaveOptions { get; set; } = [];

    /// <summary>
    /// 表示本次统计实际使用的波次号。
    /// </summary>
    public string? SelectedWaveCode { get; set; }

    /// <summary>
    /// 表示各码头维度的统计结果列表。
    /// </summary>
    public IReadOnlyList<DockDashboardSummaryResponse> DockSummaries { get; set; } = [];
}

