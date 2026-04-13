using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 扫描匹配服务抽象，负责按条码定位与之关联的业务任务。
/// </summary>
public interface IScanMatchService
{
    /// <summary>
    /// 按条码匹配业务任务。
    /// </summary>
    /// <param name="barcode">扫描条码文本。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>匹配结果。</returns>
    Task<ScanMatchResult> MatchByBarcodeAsync(string barcode, CancellationToken ct);
}
