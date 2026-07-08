namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskWaveOptionRow 类型。
/// </summary>
public sealed class BusinessTaskWaveOptionRow
{
    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }
}

