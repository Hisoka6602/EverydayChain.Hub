using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskQueryItem 类型。
/// </summary>
public sealed class BusinessTaskQueryItem
{
    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; }

    /// <summary>
    /// 获取或设置 Status。
    /// </summary>
    public BusinessTaskStatus Status { get; set; }

    /// <summary>
    /// 获取或设置 TargetChuteCode。
    /// </summary>
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 ActualChuteCode。
    /// </summary>
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 DockCode。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 IsRecirculated。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 获取或设置 IsException。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 获取或设置 CreatedTimeLocal。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

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
}

