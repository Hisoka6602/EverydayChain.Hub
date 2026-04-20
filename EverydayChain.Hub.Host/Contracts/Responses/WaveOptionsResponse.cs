namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 波次选项查询响应。
/// </summary>
public sealed class WaveOptionsResponse
{
    /// <summary>
    /// 查询开始时间（本地时间，包含边界）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含边界）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 波次选项集合。
    /// </summary>
    public IReadOnlyList<WaveOptionItemResponse> WaveOptions { get; set; } = [];
}
