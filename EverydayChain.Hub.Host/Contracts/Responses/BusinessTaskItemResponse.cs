namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示业务任务明细中的单条记录。
/// </summary>
public sealed class BusinessTaskItemResponse
{
    /// <summary>
    /// 表示业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示箱码或业务条码。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 表示任务来源类型。
    /// </summary>
    public int SourceType { get; set; }

    /// <summary>
    /// 表示当前任务、波次或批次的业务状态。
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 表示系统计算出的目标格口编码。
    /// </summary>
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 表示实际落格编码。
    /// </summary>
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 表示码头编码。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示任务是否发生过回流。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 表示任务是否被判定为异常件。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 表示记录创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

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
}

