namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ScanLogRecognitionAggregate
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TotalScanCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MatchedScanCount { get; set; }
}

