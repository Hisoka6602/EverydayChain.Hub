using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IScanMatchService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<ScanMatchResult> MatchByBarcodeAsync(string barcode, CancellationToken ct);
}

