using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;

/// <summary>
/// 分拣任务追踪实体，用于记录中台分拣过程中各节点的业务状态快照。
/// </summary>
public class SortingTaskTraceEntity : IEntity<long>
{
    /// <summary>
    /// 实体唯一主键（自增 BIGINT）。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 业务单号，最大 32 字符。
    /// </summary>
    [MaxLength(32)]
    public string BusinessNo { get; set; } = string.Empty;

    /// <summary>
    /// 渠道标识，最大 32 字符。
    /// </summary>
    [MaxLength(32)]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// 站点编码，最大 64 字符。
    /// </summary>
    [MaxLength(64)]
    public string StationCode { get; set; } = string.Empty;

    /// <summary>
    /// 当前状态，最大 32 字符。
    /// </summary>
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 记录创建时间（含本地偏移）。
    /// 赋值时必须使用 <c>DateTimeOffset.Now</c>（本地时间偏移），禁止使用 <c>DateTimeOffset.UtcNow</c> 或任何 UTC 语义。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 业务载荷 JSON，最大 512 字符，可为空。
    /// </summary>
    [MaxLength(512)]
    public string? Payload { get; set; }
}
