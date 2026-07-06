using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DashboardTaskSnapshotEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(128)]
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string ResolvedDockCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(32)]
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime BucketStartLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskStatus Status { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ScannedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RequiredFeedbackCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CompletedFeedbackCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal TotalVolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal TotalWeightGram { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime EarliestCreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime LatestUpdatedTimeLocal { get; set; }
}

