namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务查询分页结果。
/// </summary>
public sealed class BusinessTaskQueryResult
{
    /// <summary>
    /// 总记录数。
    /// 空值语义：游标分页模式下固定返回 -1，表示不执行全量计数。
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

    /// <summary>
    /// 是否还有下一页。
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// 下一页游标：最后一条创建时间（本地时间）。
    /// </summary>
    public DateTime? NextLastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 下一页游标：最后一条主键 Id。
    /// </summary>
    public long? NextLastId { get; set; }

    /// <summary>
    /// 分页模式（可填写项：PageNumber、Cursor）。
    /// </summary>
    public string PaginationMode { get; set; } = "PageNumber";
}
