using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;

/// <summary>
/// 定义 DashboardSnapshotStateEntity 类型。
/// </summary>
public sealed class DashboardSnapshotStateEntity : IEntity<DashboardSnapshotSource>
{
    /// <summary>
    /// 获取或设置 Id。
    /// </summary>
    public DashboardSnapshotSource Id { get; set; }

    /// <summary>
    /// 获取或设置 CoverageStartLocal。
    /// </summary>
    public DateTime? CoverageStartLocal { get; set; }

    /// <summary>
    /// 获取或设置 CoverageEndLocal。
    /// </summary>
    public DateTime? CoverageEndLocal { get; set; }

    /// <summary>
    /// 获取或设置 LastRefreshTimeLocal。
    /// </summary>
    public DateTime? LastRefreshTimeLocal { get; set; }
}

