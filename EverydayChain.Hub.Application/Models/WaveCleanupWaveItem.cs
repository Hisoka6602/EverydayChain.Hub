namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveCleanupWaveItem 类型。
/// </summary>
public sealed class WaveCleanupWaveItem
{
    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 PackageTotal。
    /// </summary>
    public int PackageTotal { get; set; }

    /// <summary>
    /// 获取或设置 SplitTotal。
    /// </summary>
    public int SplitTotal { get; set; }

    /// <summary>
    /// 获取或设置 FullCaseTotal。
    /// </summary>
    public int FullCaseTotal { get; set; }

    /// <summary>
    /// 获取或设置 CreatedTimeLocal。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 Status。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

