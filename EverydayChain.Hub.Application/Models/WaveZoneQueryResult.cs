namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 波次分区查询结果。
/// </summary>
public sealed class WaveZoneQueryResult
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
    public IReadOnlyList<WaveZoneSummary> Zones { get; set; } = [];
}
