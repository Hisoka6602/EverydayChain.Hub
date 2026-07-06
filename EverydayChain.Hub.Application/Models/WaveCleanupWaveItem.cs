namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveCleanupWaveItem
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PackageTotal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SplitTotal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int FullCaseTotal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

