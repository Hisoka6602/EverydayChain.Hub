namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示波次下拉选项查询结果。
/// </summary>
public sealed class WaveOptionsResponse
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
    /// 表示可供前端筛选的波次列表。
    /// </summary>
    public IReadOnlyList<WaveOptionItemResponse> WaveOptions { get; set; } = [];
}

