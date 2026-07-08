using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskProjectionRow 类型。
/// </summary>
public class BusinessTaskProjectionRow
{
    /// <summary>
    /// 获取或设置 SourceTableCode。
    /// </summary>
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

    /// <summary>
    /// 获取或设置 BusinessKey。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 WorkingArea。
    /// </summary>
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置 OrderId。
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// 获取或设置 StoreId。
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// 获取或设置 StoreName。
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// 获取或设置 ProductCode。
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// 获取或设置 PickLocation。
    /// </summary>
    public string? PickLocation { get; set; }

    /// <summary>
    /// 获取或设置 ProjectedTimeLocal。
    /// </summary>
    public required DateTime ProjectedTimeLocal { get; set; }
}

