namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 波次选项项。
/// </summary>
public sealed class WaveOptionItem
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
