using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.AuditLogs;

/// <summary>
/// 表示一次保留期清理目标执行记录。
/// 该审计实体用于追踪后台自动清理针对哪个逻辑表执行了何种治理模式，以及本次扫描、识别和删除的结果。
/// </summary>
public sealed class RetentionCleanupAuditLogEntity : IEntity<string>
{
    /// <summary>
    /// 获取或设置审计记录唯一标识。
    /// </summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置同一轮保留期任务的批次标识。
    /// 用于将同一轮后台清理过程中产生的多条目标记录聚合到一起。
    /// </summary>
    [MaxLength(32)]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置清理目标编码。
    /// 对业务主表来源通常是同步表编码，对固定表或日志表则使用系统生成的目标编码。
    /// </summary>
    [MaxLength(64)]
    public string TargetCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置逻辑表名或固定表名。
    /// </summary>
    [MaxLength(128)]
    public string LogicalTableName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次执行采用的保留模式。
    /// 可选值为 DropShards 或 DeleteRows。
    /// </summary>
    [MaxLength(32)]
    public string RetentionMode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置按时间删行模式使用的时间列名。
    /// 分表删除模式下该字段允许为空字符串。
    /// </summary>
    [MaxLength(64)]
    public string TimeColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次执行的保留月数配置。
    /// </summary>
    public int KeepMonths { get; set; }

    /// <summary>
    /// 获取或设置本次执行是否为预演模式。
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// 获取或设置本次执行是否允许真正删除。
    /// </summary>
    public bool AllowDelete { get; set; }

    /// <summary>
    /// 获取或设置本次执行采用的时间阈值。
    /// 早于该阈值的旧分表或旧数据会成为清理候选。
    /// </summary>
    public DateTime ThresholdTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前审计记录的执行阶段。
    /// 可选值为 Started、Completed 或 Failed。
    /// </summary>
    [MaxLength(16)]
    public string ExecutionStage { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次执行扫描到的对象数量。
    /// 对分表模式表示扫描到的物理分表数量，对删行模式固定表示单表。
    /// </summary>
    public int ScannedCount { get; set; }

    /// <summary>
    /// 获取或设置本次执行识别出的过期候选数量。
    /// 对分表模式表示过期分表数，对删行模式表示过期数据行数。
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// 获取或设置本次执行实际删除的数量。
    /// 对分表模式表示删除的分表数，对删行模式表示删除的数据行数。
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// 获取或设置本次执行的结果说明。
    /// </summary>
    [MaxLength(512)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置执行实例标识。
    /// 用于识别是哪台宿主机上的哪个进程执行了本次清理。
    /// </summary>
    [MaxLength(128)]
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次执行开始时间。
    /// </summary>
    public DateTime StartedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置本次执行完成时间。
    /// 失败或异常结束时同样会记录结束时间。
    /// </summary>
    public DateTime? CompletedTimeLocal { get; set; }
}
