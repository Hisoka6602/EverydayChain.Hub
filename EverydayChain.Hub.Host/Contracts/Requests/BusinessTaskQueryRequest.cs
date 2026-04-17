namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 业务任务查询请求。
/// </summary>
public sealed class BusinessTaskQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含）。
    /// 可填写范围：必须大于 DateTime.MinValue。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含）。
    /// 可填写范围：必须大于开始时间。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 波次号筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 条码筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 码头号筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// </summary>
    public string? DockCode { get; set; }

    /// <summary>
    /// 格口号筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 页码（从 1 开始）。
    /// 可填写范围：1~100000。
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 页大小。
    /// 可填写范围：1~1000。
    /// </summary>
    public int PageSize { get; set; } = 50;
}
