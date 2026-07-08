namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveZoneQueryResult 类型。
/// </summary>
public sealed class WaveZoneQueryResult
{
    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 Zones。
    /// </summary>
    public IReadOnlyList<WaveZoneSummary> Zones { get; set; } = [];
}

