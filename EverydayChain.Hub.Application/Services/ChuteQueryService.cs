using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Utilities;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义 ChuteQueryService 类型。
/// </summary>
public sealed class ChuteQueryService : IChuteQueryService
{
    /// <summary>
    /// 存储 NullCacheValue 字段。
    /// </summary>
    private const string NullCacheValue = "_";

    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 存储 _barcodeParser 字段。
    /// </summary>
    private readonly IBarcodeParser _barcodeParser;

    /// <summary>
    /// 存储 _memoryCache 字段。
    /// </summary>
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// 存储 _queryCacheOptions 字段。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;

    public ChuteQueryService(IBusinessTaskRepository businessTaskRepository, IBarcodeParser barcodeParser)
        : this(
            businessTaskRepository,
            barcodeParser,
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions())
    {
    }

    public ChuteQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IBarcodeParser barcodeParser,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        _businessTaskRepository = businessTaskRepository;
        _barcodeParser = barcodeParser;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    public async Task<ChuteResolveApplicationResult> ExecuteAsync(ChuteResolveApplicationRequest request, CancellationToken cancellationToken)
    {
        var normalizedTaskCode = string.IsNullOrWhiteSpace(request.TaskCode) ? null : request.TaskCode.Trim();
        var normalizedBarcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

        if (_queryCacheOptions.Enabled)
        {
            var cacheKey = BuildCacheKey(normalizedTaskCode, normalizedBarcode);
            var ttlSeconds = Math.Clamp(_queryCacheOptions.ChuteResolveSeconds, 1, 60);
            var cached = await MemoryCacheSingleFlight.GetOrCreateAsync(
                _memoryCache,
                cacheKey,
                TimeSpan.FromSeconds(ttlSeconds),
                _ => ExecuteCoreAsync(normalizedTaskCode, normalizedBarcode, CancellationToken.None),
                cancellationToken);

            if (cached is not null)
            {
                return cached;
            }
        }

        return await ExecuteCoreAsync(normalizedTaskCode, normalizedBarcode, cancellationToken);
    }

    private async Task<ChuteResolveApplicationResult> ExecuteCoreAsync(
        string? normalizedTaskCode,
        string? normalizedBarcode,
        CancellationToken cancellationToken)
    {
        var task = normalizedTaskCode is not null
            ? await _businessTaskRepository.FindByTaskCodeAsync(normalizedTaskCode, cancellationToken)
            : null;

        if (task == null && normalizedBarcode is not null)
        {
            task = await _businessTaskRepository.FindByBarcodeAsync(normalizedBarcode, cancellationToken);
        }

        if (task == null)
        {
            return new ChuteResolveApplicationResult
            {
                IsResolved = false,
                TaskCode = string.Empty,
                ChuteCode = string.Empty,
                Message = $"未找到条码 [{normalizedBarcode}] 或任务编码 [{normalizedTaskCode}] 对应的业务任务。"
            };
        }

        if (task.Status != BusinessTaskStatus.Scanned && task.Status != BusinessTaskStatus.Dropped)
        {
            return new ChuteResolveApplicationResult
            {
                IsResolved = false,
                TaskCode = task.TaskCode,
                ChuteCode = string.Empty,
                Message = $"任务 [{task.TaskCode}] 当前状态 [{ChineseDisplayText.ForTaskStatus(task.Status)}] 不允许请求格口，仅已扫描或已落格任务可查询格口。"
            };
        }

        var normalizedTargetChuteCode = string.IsNullOrWhiteSpace(task.TargetChuteCode) ? null : task.TargetChuteCode.Trim();
        if (normalizedTargetChuteCode is not null)
        {
            return new ChuteResolveApplicationResult
            {
                IsResolved = true,
                TaskCode = task.TaskCode,
                ChuteCode = normalizedTargetChuteCode,
                Message = $"任务 [{task.TaskCode}] 目标格口已确认：{normalizedTargetChuteCode}。"
            };
        }

        if (string.IsNullOrWhiteSpace(task.Barcode))
        {
            return new ChuteResolveApplicationResult
            {
                IsResolved = false,
                TaskCode = task.TaskCode,
                ChuteCode = string.Empty,
                Message = $"任务 [{task.TaskCode}] 条码为空，无法解析目标格口。"
            };
        }

        var barcodeForResolve = task.Barcode;
        var parseResult = _barcodeParser.Parse(barcodeForResolve);
        if (!parseResult.IsValid)
        {
            return new ChuteResolveApplicationResult
            {
                IsResolved = false,
                TaskCode = task.TaskCode,
                ChuteCode = string.Empty,
                Message = $"任务 [{task.TaskCode}] 条码 [{barcodeForResolve}] 未携带受支持的目标格口信息。"
            };
        }

        return new ChuteResolveApplicationResult
        {
            IsResolved = true,
            TaskCode = task.TaskCode,
            ChuteCode = parseResult.TargetChuteCode,
            Message = $"任务 [{task.TaskCode}] 目标格口已确认：{parseResult.TargetChuteCode}。"
        };
    }

    private static string BuildCacheKey(string? normalizedTaskCode, string? normalizedBarcode)
    {
        return string.Join(':',
            "chute-resolve",
            NormalizeCacheSegment(normalizedTaskCode),
            NormalizeCacheSegment(normalizedBarcode));
    }

    private static string NormalizeCacheSegment(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? NullCacheValue
            : value.Trim();
    }
}
