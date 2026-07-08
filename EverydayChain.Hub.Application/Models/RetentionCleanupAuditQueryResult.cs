namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 表示保留期清理审计分页查询结果。
/// </summary>
public sealed class RetentionCleanupAuditQueryResult
{
    /// <summary>
    /// 获取或设置命中总数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置当前页码。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 获取或设置当前页大小。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置当前页记录集合。
    /// </summary>
    public IReadOnlyList<RetentionCleanupAuditItem> Items { get; set; } = [];
}
