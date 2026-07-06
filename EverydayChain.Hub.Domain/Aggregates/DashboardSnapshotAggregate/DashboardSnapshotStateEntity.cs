using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DashboardSnapshotStateEntity : IEntity<DashboardSnapshotSource>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DashboardSnapshotSource Id { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? CoverageStartLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? CoverageEndLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? LastRefreshTimeLocal { get; set; }
}

