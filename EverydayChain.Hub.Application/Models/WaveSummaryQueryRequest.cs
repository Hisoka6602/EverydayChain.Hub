namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveSummaryQueryRequest 类型。
/// </summary>
public sealed class WaveSummaryQueryRequest
{
    /// <summary>
    /// 获取或设置 StartTimeLocal。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 EndTimeLocal。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;
}

