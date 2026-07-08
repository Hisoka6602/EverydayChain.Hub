namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 表示箱子追踪页的一条扫描与任务关联结果。
/// 该对象用于前端展示，不改变既有分拣机扫描、匹配和落格处理语义。
/// </summary>
public sealed class BoxTrackingItem
{
    /// <summary>
    /// 获取或设置箱子追踪页展示的箱码值。
    /// 该值直接来源于扫描日志中的 Barcode。
    /// </summary>
    public string BoxId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string? WaveCode { get; set; }

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
    /// 获取或设置扫描设备编码。
    /// </summary>
    public string? Scanner { get; set; }

    /// <summary>
    /// 获取或设置 ScannedAtLocal。
    /// </summary>
    public DateTime ScannedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置用于页面展示的格口编码。
    /// </summary>
    public string? Chute { get; set; }

    /// <summary>
    /// 获取或设置箱子追踪页状态码。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 IsMatched。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    public string? FailureReason { get; set; }
}

