namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskRecirculationAggregateRow 类型。
/// </summary>
public sealed class BusinessTaskRecirculationAggregateRow
{
    /// <summary>
    /// 获取或设置 ChuteCode。
    /// </summary>
    public string ChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 RecirculatedCount。
    /// </summary>
    public int RecirculatedCount { get; set; }
}

