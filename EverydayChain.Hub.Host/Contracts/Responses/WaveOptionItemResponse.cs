namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 波次选项项响应。
/// </summary>
public sealed class WaveOptionItemResponse
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
