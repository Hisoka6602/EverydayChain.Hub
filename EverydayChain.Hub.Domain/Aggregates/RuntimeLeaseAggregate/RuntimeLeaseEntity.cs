using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.RuntimeLeaseAggregate;

/// <summary>
/// 表示运行时租约实体。
/// </summary>
public sealed class RuntimeLeaseEntity : IEntity<string>
{
    /// <summary>
    /// 获取或设置租约键。
    /// </summary>
    [Key]
    [MaxLength(128)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前持有租约的实例标识。
    /// </summary>
    [MaxLength(64)]
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本次成功获取租约的本地时间。
    /// </summary>
    public DateTime AcquiredTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置租约过期的本地时间。
    /// </summary>
    public DateTime ExpiresAtLocal { get; set; }
}
