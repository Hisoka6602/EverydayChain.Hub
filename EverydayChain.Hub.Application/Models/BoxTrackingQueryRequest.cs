namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 表示箱子追踪查询条件。
/// 应用层保留 boxId 这一字段命名，但它对应的仍然是扫描日志中的 Barcode。
/// </summary>
public sealed class BoxTrackingQueryRequest
{
    /// <summary>
    /// 获取或设置 StartTimeLocal。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 EndTimeLocal。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置箱子追踪页使用的箱码查询值。
    /// 该字段实际筛选扫描日志中的 Barcode。
    /// </summary>
    public string? BoxId { get; set; }

    /// <summary>
    /// 获取或设置 OrderId。
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// 获取或设置 StoreId。
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// 获取或设置扫描设备编码。
    /// </summary>
    public string? Scanner { get; set; }

    /// <summary>
    /// 获取或设置格口筛选值。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 PageNumber。
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 获取或设置 PageSize。
    /// </summary>
    public int PageSize { get; set; } = 50;
}

