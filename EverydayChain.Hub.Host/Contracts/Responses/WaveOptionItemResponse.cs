namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示波次下拉选项中的单个候选波次。
/// </summary>
public sealed class WaveOptionItemResponse
{
    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }
}

