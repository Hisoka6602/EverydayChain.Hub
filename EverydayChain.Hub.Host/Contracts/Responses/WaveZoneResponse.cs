namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示波次分区汇总查询结果。
/// </summary>
public sealed class WaveZoneResponse
{
    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 表示当前波次下各分区的统计结果列表。
    /// </summary>
    public IReadOnlyList<WaveZoneSummaryResponse> Zones { get; set; } = [];
}

