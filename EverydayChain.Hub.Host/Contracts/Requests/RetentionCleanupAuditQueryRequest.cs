namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示保留期清理审计查询请求。
/// </summary>
public sealed class RetentionCleanupAuditQueryRequest
{
    /// <summary>
    /// 获取或设置查询开始时间，使用本地时间。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置查询结束时间，使用本地时间。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置逻辑表名筛选条件。
    /// </summary>
    public string? LogicalTableName { get; set; }

    /// <summary>
    /// 获取或设置目标编码筛选条件。
    /// </summary>
    public string? TargetCode { get; set; }

    /// <summary>
    /// 获取或设置执行阶段筛选条件。
    /// 可选值通常为 Started、Completed 或 Failed。
    /// </summary>
    public string? ExecutionStage { get; set; }

    /// <summary>
    /// 获取或设置批次号筛选条件。
    /// </summary>
    public string? BatchId { get; set; }

    /// <summary>
    /// 获取或设置分页页码，从 1 开始。
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 获取或设置每页返回条数。
    /// </summary>
    public int PageSize { get; set; } = 50;
}
