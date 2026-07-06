namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveZoneResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<WaveZoneSummaryResponse> Zones { get; set; } = [];
}

