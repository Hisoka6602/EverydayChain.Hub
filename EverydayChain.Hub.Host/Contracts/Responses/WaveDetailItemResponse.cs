namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示波次明细中的单条业务任务记录。
/// </summary>
public sealed class WaveDetailItemResponse
{
    /// <summary>
    /// 表示业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 表示任务来源类型。
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// 表示拆零作业所属的工作区域。
    /// </summary>
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 表示箱码或业务条码。
    /// </summary>
    public string? Barcode { get; set; }

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
    /// 表示格口编码。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 表示当前任务、波次或批次的业务状态。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 表示任务是否发生过回流。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 表示任务是否被判定为异常件。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 表示条码扫描完成时间（本地时间）。
    /// </summary>
    public DateTime? ScannedAt { get; set; }

    /// <summary>
    /// 表示记录创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 表示记录最后更新时间（本地时间）。
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

