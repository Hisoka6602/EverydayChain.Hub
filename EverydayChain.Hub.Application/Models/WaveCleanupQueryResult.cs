namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveCleanupQueryResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<WaveCleanupWaveItem> Items { get; set; } = [];
}

