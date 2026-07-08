using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IScanMatchService 类型。
/// </summary>
public interface IScanMatchService
{
    /// <summary>
    /// 执行 MatchByBarcodeAsync 方法。
    /// </summary>
    Task<ScanMatchResult> MatchByBarcodeAsync(string barcode, CancellationToken ct);
}

