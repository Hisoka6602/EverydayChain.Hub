using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.AuditLogs;

/// <summary>
/// 表示一次波次清理敏感操作的审计记录。
/// 该记录用于追踪谁在什么时间对哪个波次发起了正式清理，以及本次操作最终影响了多少业务任务。
/// </summary>
public sealed class WaveCleanupAuditLogEntity : IEntity<string>
{
    /// <summary>
    /// 获取或设置审计记录唯一标识。
    /// </summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置发起清理的波次号。
    /// </summary>
    [MaxLength(64)]
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次操作要写入的目标业务状态。
    /// </summary>
    [MaxLength(32)]
    public string TargetStatus { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置审计记录当前阶段。
    /// 可选值为 Started、Completed、Failed。
    /// </summary>
    [MaxLength(16)]
    public string ExecutionStage { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次请求识别出的待处理任务数量。
    /// </summary>
    public int IdentifiedCount { get; set; }

    /// <summary>
    /// 获取或设置本次请求实际写入变更的任务数量。
    /// </summary>
    public int CleanedCount { get; set; }

    /// <summary>
    /// 获取或设置本次操作结果说明。
    /// </summary>
    [MaxLength(512)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置发起请求时的本地时间。
    /// </summary>
    public DateTime RequestedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置本次操作完成时的本地时间。
    /// </summary>
    public DateTime? CompletedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置请求链路跟踪标识。
    /// </summary>
    [MaxLength(128)]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置调用接口的请求路径。
    /// </summary>
    [MaxLength(128)]
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置调用接口的请求方法。
    /// </summary>
    [MaxLength(16)]
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置调用方自报的操作人标识。
    /// 如果调用方未传递，则记录为空字符串。
    /// </summary>
    [MaxLength(64)]
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置客户端 IP 地址。
    /// </summary>
    [MaxLength(64)]
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置客户端 User-Agent。
    /// </summary>
    [MaxLength(256)]
    public string UserAgent { get; set; } = string.Empty;
}
