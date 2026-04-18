namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务查询请求。
/// </summary>
public sealed class BusinessTaskQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 波次号筛选。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 条码筛选。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 码头筛选。
    /// </summary>
    public string? DockCode { get; set; }

    /// <summary>
    /// 格口筛选。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 页码（从 1 开始）。
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 页大小。
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// 游标分页：上一页最后一条的创建时间（本地时间）。
    /// </summary>
    public DateTime? LastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 游标分页：上一页最后一条的主键 Id。
    /// </summary>
    public long? LastId { get; set; }
}
