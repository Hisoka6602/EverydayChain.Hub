namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示业务任务、异常件或回流件明细查询条件。
/// </summary>
public sealed class BusinessTaskQueryRequest
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
    /// 表示波次号。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 表示箱码或业务条码。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 表示码头编码。
    /// </summary>
    public string? DockCode { get; set; }

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

    /// <summary>
    /// 表示游标翻页请求使用的最后创建时间锚点。
    /// </summary>
    public DateTime? LastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 表示游标翻页请求使用的最后主键锚点。
    /// </summary>
    public long? LastId { get; set; }
}

