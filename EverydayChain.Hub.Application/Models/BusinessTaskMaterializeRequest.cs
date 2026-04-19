using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务物化输入模型，承载从同步链路传入的最小字段。
/// </summary>
public class BusinessTaskMaterializeRequest
{
    /// <summary>
    /// 任务编码（可填写范围：1~64 个字符，不能为空白）。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 来源同步表编码（可填写范围：1~64 个字符，不能为空白）。
    /// </summary>
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 业务键文本（可填写范围：1~256 个字符，不能为空白）。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本（可填写范围：1~128 个字符，可为空）。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 来源类型（可填写范围：Unknown、Split、FullCase）。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

    /// <summary>
    /// 波次编码（可填写范围：1~64 个字符，可为空）。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 波次备注（可填写范围：1~128 个字符，可为空）。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 物化时间（本地时间）；正式业务写入必须显式提供，不允许缺失。
    /// </summary>
    public DateTime? MaterializedTimeLocal { get; set; }
}
