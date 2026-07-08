using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskWaveTaskStatsRow 类型。
/// </summary>
public sealed class BusinessTaskWaveTaskStatsRow
{
    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; }

    /// <summary>
    /// 获取或设置 WorkingArea。
    /// </summary>
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置 Status。
    /// </summary>
    public BusinessTaskStatus Status { get; set; }

    /// <summary>
    /// 获取或设置 ResolvedDockCode。
    /// </summary>
    public string ResolvedDockCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 IsException。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 UpdatedTimeLocal。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }
}

