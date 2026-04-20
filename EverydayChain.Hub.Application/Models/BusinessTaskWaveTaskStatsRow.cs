using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 波次统计所需的业务任务最小字段投影行。
/// </summary>
public sealed class BusinessTaskWaveTaskStatsRow
{
    /// <summary>
    /// 来源类型。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; }

    /// <summary>
    /// 工作区域编码。
    /// </summary>
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 任务状态。
    /// </summary>
    public BusinessTaskStatus Status { get; set; }

    /// <summary>
    /// 归并码头编码。
    /// </summary>
    public string ResolvedDockCode { get; set; } = string.Empty;

    /// <summary>
    /// 是否异常。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 更新时间（本地时间）。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }
}
