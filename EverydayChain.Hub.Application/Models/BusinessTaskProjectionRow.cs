using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务投影行，描述从远端状态驱动读取后可投影到业务任务主表的字段集合。
/// </summary>
public class BusinessTaskProjectionRow
{
    /// <summary>
    /// 来源同步表编码。
    /// </summary>
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 业务来源类型。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

    /// <summary>
    /// 业务键。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 条码。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 波次号。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 工作区域编码。
    /// </summary>
    public string? WorkingArea { get; set; }

    /// <summary>
    /// Upstream order identifier projected from the remote source row.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Upstream store identifier projected from the remote source row.
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// Upstream store display name projected from the remote source row.
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Upstream product code projected from the remote source row.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Upstream pick location projected from the remote source row.
    /// </summary>
    public string? PickLocation { get; set; }

    /// <summary>
    /// 投影时间（本地时间）。
    /// </summary>
    public required DateTime ProjectedTimeLocal { get; set; }
}
