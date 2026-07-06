using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;

/// <summary>
/// 表示当前波次分钟快照实体。
/// </summary>
public sealed class DashboardCurrentWaveSnapshotEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置快照主键。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置分钟桶开始时间。
    /// </summary>
    public DateTime BucketStartLocal { get; set; }

    /// <summary>
    /// 获取或设置该分钟内最新一次扫描时间。
    /// </summary>
    public DateTime ScannedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置归一化后的波次号。
    /// </summary>
    [MaxLength(64)]
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置波次备注。
    /// </summary>
    [MaxLength(128)]
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置用于展示的条码值。
    /// </summary>
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;
}
