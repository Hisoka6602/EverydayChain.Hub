namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 表示单条保留期清理审计明细。
/// </summary>
public sealed class RetentionCleanupAuditItem
{
    /// <summary>
    /// 获取或设置审计记录标识。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置批次标识。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置清理目标编码。
    /// </summary>
    public string TargetCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置逻辑表名。
    /// </summary>
    public string LogicalTableName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置保留模式。
    /// 取值通常为 DropShards 或 DeleteRows。
    /// </summary>
    public string RetentionMode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置按时间删行模式使用的时间列名。
    /// </summary>
    public string TimeColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置保留月数。
    /// </summary>
    public int KeepMonths { get; set; }

    /// <summary>
    /// 获取或设置当前执行是否为预演模式。
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// 获取或设置当前执行是否允许真正删除数据。
    /// </summary>
    public bool AllowDelete { get; set; }

    /// <summary>
    /// 获取或设置当前执行阶段。
    /// </summary>
    public string ExecutionStage { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置扫描数量。
    /// </summary>
    public int ScannedCount { get; set; }

    /// <summary>
    /// 获取或设置候选数量。
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// 获取或设置实际删除数量。
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// 获取或设置执行结果说明。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置执行实例标识。
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次执行采用的阈值时间。
    /// </summary>
    public DateTime ThresholdTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置开始时间。
    /// </summary>
    public DateTime StartedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置完成时间。
    /// </summary>
    public DateTime? CompletedTimeLocal { get; set; }
}
