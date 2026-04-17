using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务查询结果项。
/// </summary>
public sealed class BusinessTaskQueryItem
{
    /// <summary>
    /// 任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 波次号。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 来源类型。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; }

    /// <summary>
    /// 任务状态。
    /// </summary>
    public BusinessTaskStatus Status { get; set; }

    /// <summary>
    /// 目标格口号。
    /// </summary>
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 实际格口号。
    /// </summary>
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 码头号。
    /// </summary>
    public string DockCode { get; set; } = string.Empty;

    /// <summary>
    /// 是否回流。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 是否异常。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }
}
