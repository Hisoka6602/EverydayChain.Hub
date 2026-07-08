namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveListQueryRequest 类型。
/// </summary>
public sealed class WaveListQueryRequest
{
    /// <summary>
    /// 获取或设置 StartTimeLocal。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 EndTimeLocal。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }
}

