using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DashboardScanSnapshotEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime BucketStartLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TotalScanCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MatchedScanCount { get; set; }
}

