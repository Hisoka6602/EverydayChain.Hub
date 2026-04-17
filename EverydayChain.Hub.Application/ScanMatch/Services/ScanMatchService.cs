using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.ScanMatch.Services;

/// <summary>
/// 扫描匹配服务实现，按条码在业务任务仓储中定位任务。
/// </summary>
public sealed class ScanMatchService : IScanMatchService
{
    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 初始化扫描匹配服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    public ScanMatchService(IBusinessTaskRepository businessTaskRepository)
    {
        _businessTaskRepository = businessTaskRepository;
    }

    /// <summary>
    /// 按条码匹配业务任务。匹配成功返回任务实体；任务不存在时返回未匹配结果。
    /// </summary>
    /// <param name="barcode">扫描条码文本。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>匹配结果。</returns>
    public async Task<ScanMatchResult> MatchByBarcodeAsync(string barcode, CancellationToken ct)
    {
        var normalizedBarcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBarcode))
        {
            return new ScanMatchResult
            {
                IsMatched = false,
                FailureReason = "条码不能为空白。"
            };
        }

        var task = await _businessTaskRepository.FindByBarcodeAsync(normalizedBarcode, ct);
        if (task == null)
        {
            return new ScanMatchResult
            {
                IsMatched = false,
                FailureReason = $"未找到条码 [{normalizedBarcode}] 对应的业务任务。"
            };
        }

        return new ScanMatchResult
        {
            IsMatched = true,
            Task = task
        };
    }
}
