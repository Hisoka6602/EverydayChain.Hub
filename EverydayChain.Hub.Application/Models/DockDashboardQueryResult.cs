namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 DockDashboardQueryResult 类型。
/// </summary>
public sealed class DockDashboardQueryResult
{
    /// <summary>
    /// 获取或设置 StartTimeLocal。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 EndTimeLocal。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 WaveOptions。
    /// </summary>
    public IReadOnlyList<string> WaveOptions { get; set; } = [];

    /// <summary>
    /// 获取或设置 SelectedWaveCode。
    /// </summary>
    public string? SelectedWaveCode { get; set; }

    /// <summary>
    /// 获取或设置 DockSummaries。
    /// </summary>
    public IReadOnlyList<DockDashboardSummary> DockSummaries { get; set; } = [];
}

