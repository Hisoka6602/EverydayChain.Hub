namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示业务任务分页查询结果。
/// </summary>
public sealed class BusinessTaskQueryResponse
{
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
    public IReadOnlyList<BusinessTaskItemResponse> Items { get; set; } = [];

    /// <summary>
    /// 表示当前结果后续是否仍有更多数据。
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// 表示游标翻页下一页使用的创建时间锚点。
    /// </summary>
    public DateTime? NextLastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 表示游标翻页下一页使用的主键锚点。
    /// </summary>
    public long? NextLastId { get; set; }

    /// <summary>
    /// 表示当前结果采用的分页模式。
    /// </summary>
    public string PaginationMode { get; set; } = "PageNumber";
}

