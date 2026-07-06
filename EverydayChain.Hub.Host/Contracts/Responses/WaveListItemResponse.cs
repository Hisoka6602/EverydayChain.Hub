namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveListItemResponse
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
    public int UnsortedCount { get; set; }

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
    public decimal SplitRatioPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal FullRatioPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

