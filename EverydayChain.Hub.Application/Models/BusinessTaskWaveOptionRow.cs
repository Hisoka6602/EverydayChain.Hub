namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务波次选项聚合行。
/// </summary>
public sealed class BusinessTaskWaveOptionRow
{
    /// <summary>
    /// 波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }
}
