namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务查询分页结果。
/// </summary>
public sealed class BusinessTaskQueryResult
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
    public IReadOnlyList<BusinessTaskQueryItem> Items { get; set; } = [];
}
