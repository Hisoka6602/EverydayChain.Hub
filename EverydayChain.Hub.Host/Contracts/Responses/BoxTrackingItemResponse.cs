namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示箱子追踪结果中的单条扫描与匹配记录。
/// </summary>
public sealed class BoxTrackingItemResponse
{
    /// <summary>
    /// 表示箱子追踪页展示的箱码值。
    /// 该值直接来自扫描日志中的 Barcode，保留现有字段名以兼容既有页面。
    /// </summary>
    public string BoxId { get; set; } = string.Empty;

    /// <summary>
    /// 表示业务任务编码。
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 表示订单标识。
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// 表示门店标识。
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// 表示门店名称。
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// 表示商品编码。
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// 表示拣货位编码。
    /// </summary>
    public string? PickLocation { get; set; }

    /// <summary>
    /// 表示执行本次扫描的设备编码。
    /// 页面通常会将它展示为读码设备。
    /// </summary>
    public string? Scanner { get; set; }

    /// <summary>
    /// 表示条码扫描完成时间（本地时间）。
    /// </summary>
    public DateTime ScannedAt { get; set; }

    /// <summary>
    /// 表示本条记录最终用于展示的格口编码。
    /// 优先取实际落格，其次取目标格口，最后回退到码头编码。
    /// </summary>
    public string? Chute { get; set; }

    /// <summary>
    /// 表示箱子追踪页的状态文案。
    /// 当前返回值为“已扫描”“回流复扫”“异常待处理”“已匹配”“未匹配”等中文文案。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 表示扫描条码是否成功匹配到业务任务。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 表示处理失败、未命中或被拒绝的原因说明。
    /// </summary>
    public string? FailureReason { get; set; }
}

