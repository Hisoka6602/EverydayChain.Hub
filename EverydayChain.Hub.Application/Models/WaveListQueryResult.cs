namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveListQueryResult 类型。
/// </summary>
public sealed class WaveListQueryResult
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
    /// 获取或设置 Items。
    /// </summary>
    public IReadOnlyList<WaveListItem> Items { get; set; } = [];
}

