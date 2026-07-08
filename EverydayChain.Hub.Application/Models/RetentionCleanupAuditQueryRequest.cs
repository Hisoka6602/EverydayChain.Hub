namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 表示保留期清理审计查询条件。
/// 该请求用于按时间范围、逻辑表、目标编码、执行阶段和批次号筛选自动清理留痕记录。
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
    /// 同一轮后台清理产生的多条记录会共享同一个批次号。
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
