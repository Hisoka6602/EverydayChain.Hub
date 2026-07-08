namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示波次列表查询结果。
/// </summary>
public sealed class WaveListResponse
{
    /// <summary>
    /// 表示查询或统计开始时间（本地时间）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 表示查询或统计结束时间（本地时间）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 表示当前结果包含的明细列表。
    /// </summary>
    public IReadOnlyList<WaveListItemResponse> Items { get; set; } = [];
}

