namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 波次分区查询响应。
/// </summary>
public sealed class WaveZoneResponse
{
    /// <summary>
    /// 波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 分区统计集合。
    /// </summary>
    public IReadOnlyList<WaveZoneSummaryResponse> Zones { get; set; } = [];
}
