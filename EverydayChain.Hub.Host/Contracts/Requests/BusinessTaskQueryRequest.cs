namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 业务任务查询请求。
/// </summary>
public sealed class BusinessTaskQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含边界）。
    /// 可填写范围：必须大于 <see cref="DateTime.MinValue"/>；必填。
    /// 空值语义：该字段为值类型，未传时会触发时间范围校验失败。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含边界）。
    /// 可填写范围：必须大于 <see cref="StartTimeLocal"/>；必填。
    /// 空值语义：该字段为值类型，未传时会触发时间范围校验失败。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 波次号筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// 空值语义：为空时返回时间范围内全部波次。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 条码筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// 空值语义：为空时返回全部条码记录。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 码头号筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// 空值语义：为空时返回全部码头记录。
    /// </summary>
    public string? DockCode { get; set; }

    /// <summary>
    /// 格口号筛选。
    /// 可填写范围：空字符串或 null 表示不过滤。
    /// 空值语义：为空时返回全部格口记录。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 页码（从 1 开始）。
    /// 可填写范围：1~100000。
    /// 空值语义：该字段为值类型，默认值 1。
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 页大小。
    /// 可填写范围：1~1000。
    /// 空值语义：该字段为值类型，默认值 50。
    /// </summary>
    public int PageSize { get; set; } = 50;
}
