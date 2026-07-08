namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 ScanLogRecognitionAggregate 类型。
/// </summary>
public sealed class ScanLogRecognitionAggregate
{
    /// <summary>
    /// 获取或设置 TotalScanCount。
    /// </summary>
    public int TotalScanCount { get; set; }

    /// <summary>
    /// 获取或设置 MatchedScanCount。
    /// </summary>
    public int MatchedScanCount { get; set; }
}

