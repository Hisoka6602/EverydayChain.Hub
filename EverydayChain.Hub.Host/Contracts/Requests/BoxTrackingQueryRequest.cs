namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示箱子追踪查询条件。
/// 其中 boxId 是沿用页面命名的查询字段，实际表示扫描日志中的条码值。
/// </summary>
public sealed class BoxTrackingQueryRequest
{
    /// <summary>
    /// 表示查询或统计开始时间（本地时间）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 表示查询或统计结束时间（本地时间）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 表示箱子追踪页使用的箱码查询值。
    /// 该字段实际筛选扫描日志中的 Barcode，不代表新增的独立箱号主键。
    /// </summary>
    public string? BoxId { get; set; }

    /// <summary>
    /// 表示订单标识。
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// 表示门店标识。
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// 表示扫描设备标识。
    /// 当前页面示例中的读码平台名称即来源于该字段。
    /// </summary>
    public string? Scanner { get; set; }

    /// <summary>
    /// 表示格口编码。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 表示分页页码，从 1 开始计数。
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 表示每页返回的记录条数。
    /// </summary>
    public int PageSize { get; set; } = 50;
}

