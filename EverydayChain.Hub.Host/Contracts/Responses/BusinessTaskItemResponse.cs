namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 业务任务查询结果项响应。
/// </summary>
public sealed class BusinessTaskItemResponse
{
    /// <summary>
    /// 任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码。
    /// null 表示当前记录未关联可用条码。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 波次号。
    /// null 表示当前记录未分配波次。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 来源类型数值码。
    /// 可返回值：0 = Unknown（未知来源）、1 = Split（拆零）、2 = FullCase（整件）。
    /// </summary>
    public int SourceType { get; set; }

    /// <summary>
    /// 任务状态数值码。
    /// 可返回值：1 = Created（已创建）、2 = Scanned（已扫描）、3 = Dropped（已落格）、4 = FeedbackPending（待回传）、5 = Exception（异常）。
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 目标格口号。
    /// null 表示当前任务尚未分配目标格口。
    /// </summary>
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 实际格口号。
    /// null 表示尚未回传实际落格结果。
    /// </summary>
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 码头号。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 是否回流件。
    /// true 表示发生过回流；false 表示未回流。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 是否异常件。
    /// true 表示当前任务处于异常口径；false 表示非异常。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 任务创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}
