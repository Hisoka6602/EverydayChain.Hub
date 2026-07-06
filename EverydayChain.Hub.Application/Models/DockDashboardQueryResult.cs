namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DockDashboardQueryResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<string> WaveOptions { get; set; } = [];

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? SelectedWaveCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<DockDashboardSummary> DockSummaries { get; set; } = [];
}

