namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示波次清理查询结果。
/// </summary>
public sealed class WaveCleanupQueryResponse
{
    /// <summary>
    /// 表示当前结果包含的明细列表。
    /// </summary>
    public IReadOnlyList<WaveCleanupWaveItemResponse> Items { get; set; } = [];
}

