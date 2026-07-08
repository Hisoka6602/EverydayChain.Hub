namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveDetailItem 类型。
/// </summary>
public sealed class WaveDetailItem
{
    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WorkingArea。
    /// </summary>
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    public string? Barcode { get; set; }

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
    /// 获取或设置 ChuteCode。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 Status。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 IsRecirculated。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 获取或设置 IsException。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 获取或设置 ScannedAtLocal。
    /// </summary>
    public DateTime? ScannedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置 CreatedTimeLocal。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 UpdatedTimeLocal。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }
}

