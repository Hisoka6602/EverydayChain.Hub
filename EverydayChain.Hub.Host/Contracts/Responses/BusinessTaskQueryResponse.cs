namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 业务任务分页查询响应。
/// </summary>
public sealed class BusinessTaskQueryResponse
{
    /// <summary>
    /// 满足筛选条件的总记录数（不受分页影响）。
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
}
