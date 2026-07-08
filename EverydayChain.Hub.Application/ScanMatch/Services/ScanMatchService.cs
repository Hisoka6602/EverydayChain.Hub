using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.ScanMatch.Services;

/// <summary>
/// 定义 ScanMatchService 类型。
/// </summary>
public sealed class ScanMatchService : IScanMatchService
{
    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    public ScanMatchService(IBusinessTaskRepository businessTaskRepository)
    {
        _businessTaskRepository = businessTaskRepository;
    }

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

