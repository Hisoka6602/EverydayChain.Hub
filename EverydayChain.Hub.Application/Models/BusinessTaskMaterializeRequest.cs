using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskMaterializeRequest 类型。
/// </summary>
public class BusinessTaskMaterializeRequest
{
    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceTableCode。
    /// </summary>
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 BusinessKey。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 MaterializedTimeLocal。
    /// </summary>
    public required DateTime MaterializedTimeLocal { get; set; }
}

