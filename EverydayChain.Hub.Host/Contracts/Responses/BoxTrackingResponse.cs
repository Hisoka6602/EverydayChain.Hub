namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示箱子追踪分页查询结果。
/// </summary>
public sealed class BoxTrackingResponse
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
    /// 表示统计范围内的总数量。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 表示分页页码，从 1 开始计数。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 表示每页返回的记录条数。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 表示当前结果包含的明细列表。
    /// </summary>
    public IReadOnlyList<BoxTrackingItemResponse> Items { get; set; } = [];
}

