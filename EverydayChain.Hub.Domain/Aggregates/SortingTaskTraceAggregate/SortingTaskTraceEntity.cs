using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SortingTaskTraceEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(32)]
    public string BusinessNo { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(32)]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string StationCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(512)]
    public string? Payload { get; set; }
}

