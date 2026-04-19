using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 业务任务模拟补数应用服务实现。
/// </summary>
public sealed class BusinessTaskSeedService : IBusinessTaskSeedService
{
    /// <summary>
    /// 单次请求允许的最大条码数量。
    /// </summary>
    private const int MaxBarcodeCountPerRequest = 5000;

    /// <summary>
    /// 模拟补数仓储。
    /// </summary>
    private readonly IBusinessTaskSeedRepository _businessTaskSeedRepository;

    /// <summary>
    /// 日志记录器。
    /// </summary>
    private readonly ILogger<BusinessTaskSeedService> _logger;

    /// <summary>
    /// 初始化业务任务模拟补数应用服务。
    /// </summary>
    /// <param name="businessTaskSeedRepository">模拟补数仓储。</param>
    /// <param name="logger">日志记录器。</param>
    public BusinessTaskSeedService(
        IBusinessTaskSeedRepository businessTaskSeedRepository,
        ILogger<BusinessTaskSeedService> logger)
    {
        _businessTaskSeedRepository = businessTaskSeedRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<BusinessTaskSeedResult> ExecuteAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return BusinessTaskSeedResult.Fail("模拟补数请求不能为空。");
        }

        if (string.IsNullOrWhiteSpace(command.TargetTableName))
        {
            return BusinessTaskSeedResult.Fail("目标表名不能为空。");
        }

        var rawBarcodes = command.Barcodes ?? [];
        if (rawBarcodes.Count == 0)
        {
            return BusinessTaskSeedResult.Fail("条码集合不能为空。");
        }

        if (rawBarcodes.Count > MaxBarcodeCountPerRequest)
        {
            return BusinessTaskSeedResult.Fail($"单次最多允许提交 {MaxBarcodeCountPerRequest} 个条码。");
        }

        var deduplicatedBarcodes = new List<string>(rawBarcodes.Count);
        var deduplicationSet = new HashSet<string>(StringComparer.Ordinal);
        var filteredEmptyCount = 0;
        var deduplicatedCount = 0;
        foreach (var barcode in rawBarcodes)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                filteredEmptyCount++;
                continue;
            }

            var normalizedBarcode = barcode.Trim();
            if (!deduplicationSet.Add(normalizedBarcode))
            {
                deduplicatedCount++;
                continue;
            }

            deduplicatedBarcodes.Add(normalizedBarcode);
        }

        if (deduplicatedBarcodes.Count == 0)
        {
            return BusinessTaskSeedResult.Fail("条码清洗后为空，请至少提供一个有效条码。");
        }

        try
        {
            var repositoryResult = await _businessTaskSeedRepository.InsertManualSeedAsync(
                new BusinessTaskSeedCommand
                {
                    TargetTableName = command.TargetTableName.Trim(),
                    Barcodes = deduplicatedBarcodes
                },
                cancellationToken);
            repositoryResult.RequestedCount = rawBarcodes.Count;
            repositoryResult.FilteredEmptyCount = filteredEmptyCount;
            repositoryResult.DeduplicatedCount = deduplicatedCount;
            repositoryResult.CandidateCount = deduplicatedBarcodes.Count;
            return repositoryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "业务任务模拟补数执行失败。TargetTableName={TargetTableName}", command.TargetTableName);
            return BusinessTaskSeedResult.Fail("模拟补数执行失败，请稍后重试。");
        }
    }
}
