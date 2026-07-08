using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;

/// <summary>
/// 定义 DashboardScanSnapshotEntity 类型。
/// </summary>
public sealed class DashboardScanSnapshotEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置 Id。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置 BucketStartLocal。
    /// </summary>
    public DateTime BucketStartLocal { get; set; }

    /// <summary>
    /// 获取或设置 TotalScanCount。
    /// </summary>
    public int TotalScanCount { get; set; }

    /// <summary>
    /// 获取或设置 MatchedScanCount。
    /// </summary>
    public int MatchedScanCount { get; set; }
}

