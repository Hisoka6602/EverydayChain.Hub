using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.RuntimeLeaseAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class RuntimeLeaseEntity : IEntity<string>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Key]
    [MaxLength(128)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime AcquiredTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime ExpiresAtLocal { get; set; }
}

