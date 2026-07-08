namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveCleanupQueryResult 类型。
/// </summary>
public sealed class WaveCleanupQueryResult
{
    /// <summary>
    /// 获取或设置 Items。
    /// </summary>
    public IReadOnlyList<WaveCleanupWaveItem> Items { get; set; } = [];
}

