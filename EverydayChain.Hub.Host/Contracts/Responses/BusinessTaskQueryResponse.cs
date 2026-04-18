namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 业务任务分页查询响应。
/// </summary>
public sealed class BusinessTaskQueryResponse
{
    /// <summary>
    /// 满足筛选条件的总记录数（不受分页影响）。
    /// 空值语义：游标分页模式下固定返回 -1，表示不执行全量计数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码。
    /// 与请求参数 pageNumber 对齐，起始值为 1。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 页大小。
    /// 与请求参数 pageSize 对齐。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 当前页结果项集合。
    /// 为空表示该页无数据。
    /// </summary>
    public IReadOnlyList<BusinessTaskItemResponse> Items { get; set; } = [];

    /// <summary>
    /// 是否存在下一页数据。
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// 下一页游标创建时间（本地时间）。
    /// 空值语义：当 <see cref="HasMore"/> 为 false 时为空。
    /// </summary>
    public DateTime? NextLastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 下一页游标主键 Id。
    /// 空值语义：当 <see cref="HasMore"/> 为 false 时为空。
    /// </summary>
    public long? NextLastId { get; set; }

    /// <summary>
    /// 分页模式。
    /// 可填写项：PageNumber、Cursor。
    /// </summary>
    public string PaginationMode { get; set; } = "PageNumber";
}
