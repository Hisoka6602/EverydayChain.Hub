namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveCleanupWaveItemResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string WaveId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Remark { get; set; }

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
    public int FullTotal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

