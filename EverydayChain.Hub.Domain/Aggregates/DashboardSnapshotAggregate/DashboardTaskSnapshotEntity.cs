using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;

/// <summary>
/// 定义 DashboardTaskSnapshotEntity 类型。
/// </summary>
public sealed class DashboardTaskSnapshotEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置 Id。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    [MaxLength(64)]
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    [MaxLength(128)]
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 ResolvedDockCode。
    /// </summary>
    [MaxLength(64)]
    public string ResolvedDockCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WorkingArea。
    /// </summary>
    [MaxLength(32)]
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置 BucketStartLocal。
    /// </summary>
    public DateTime BucketStartLocal { get; set; }

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; }

    /// <summary>
    /// 获取或设置 Status。
    /// </summary>
    public BusinessTaskStatus Status { get; set; }

    /// <summary>
    /// 获取或设置 TotalCount。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置 ScannedCount。
    /// </summary>
    public int ScannedCount { get; set; }

    /// <summary>
    /// 获取或设置 RecirculatedCount。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置 ExceptionCount。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 获取或设置 RequiredFeedbackCount。
    /// </summary>
    public int RequiredFeedbackCount { get; set; }

    /// <summary>
    /// 获取或设置 CompletedFeedbackCount。
    /// </summary>
    public int CompletedFeedbackCount { get; set; }

    /// <summary>
    /// 获取或设置 TotalVolumeMm3。
    /// </summary>
    public decimal TotalVolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置 TotalWeightGram。
    /// </summary>
    public decimal TotalWeightGram { get; set; }

    /// <summary>
    /// 获取或设置 EarliestCreatedTimeLocal。
    /// </summary>
    public DateTime EarliestCreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 LatestUpdatedTimeLocal。
    /// </summary>
    public DateTime LatestUpdatedTimeLocal { get; set; }
}

