namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 业务任务分页查询响应。
/// </summary>
public sealed class BusinessTaskQueryResponse
{
    /// <summary>
    /// 总记录数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 页大小。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 结果项集合。
    /// </summary>
    public IReadOnlyList<BusinessTaskItemResponse> Items { get; set; } = [];
}
