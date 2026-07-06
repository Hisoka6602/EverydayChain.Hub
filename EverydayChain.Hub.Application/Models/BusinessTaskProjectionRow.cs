using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class BusinessTaskProjectionRow
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? PickLocation { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public required DateTime ProjectedTimeLocal { get; set; }
}

