namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 波次摘要查询请求。
/// </summary>
public sealed class WaveSummaryQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;
}
