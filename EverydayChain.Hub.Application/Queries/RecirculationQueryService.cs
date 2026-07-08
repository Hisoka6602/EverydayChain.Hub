using System.Text;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Utilities;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 定义 RecirculationQueryService 类型。
/// </summary>
public sealed class RecirculationQueryService : IRecirculationQueryService
{
    /// <summary>
    /// 存储 CacheKeyDateTimeFormat 字段。
    /// </summary>
    private const string CacheKeyDateTimeFormat = "yyyyMMddHHmmssfffffff";
    /// <summary>
    /// 存储 NullCacheValue 字段。
    /// </summary>
    private const string NullCacheValue = "_";

    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;
    /// <summary>
    /// 存储 _memoryCache 字段。
    /// </summary>
    private readonly IMemoryCache _memoryCache;
    /// <summary>
    /// 存储 _queryCacheOptions 字段。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;

    public RecirculationQueryService(IBusinessTaskRepository businessTaskRepository)
        : this(
            businessTaskRepository,
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions())
    {
    }

    /// <summary>
    /// 执行 RecirculationQueryService 方法。
    /// </summary>
    public RecirculationQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        // 步骤：执行 RecirculationQueryService 方法的核心处理流程。
        _businessTaskRepository = businessTaskRepository;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    public async Task<RecirculationSummaryQueryResult> QuerySummaryAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        var selectedChuteCode = NormalizeOptionalText(request.ChuteCode);
        var sortOrder = NormalizeSortOrder(request.SortOrder);
        var emptyResult = new RecirculationSummaryQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            SelectedChuteCode = selectedChuteCode,
            SortOrder = sortOrder
        };
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return emptyResult;
        }

        var cacheKey =
            $"recirculation-summary:{request.StartTimeLocal.ToString(CacheKeyDateTimeFormat)}:{request.EndTimeLocal.ToString(CacheKeyDateTimeFormat)}:{selectedChuteCode ?? NullCacheValue}:{sortOrder}";
        if (_queryCacheOptions.Enabled)
        {
            var ttl = Math.Clamp(_queryCacheOptions.RecirculationSummarySeconds, 1, 60);
            var cached = await MemoryCacheSingleFlight.GetOrCreateAsync(
                _memoryCache,
                cacheKey,
                TimeSpan.FromSeconds(ttl),
                _ => BuildSummaryAsync(request, selectedChuteCode, sortOrder, CancellationToken.None),
                cancellationToken);
            return cached ?? emptyResult;
        }

        return await BuildSummaryAsync(request, selectedChuteCode, sortOrder, cancellationToken);
    }

    public async Task<string> ExportCsvAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await QuerySummaryAsync(request, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("Chute,WaveNo,Reflow");
        foreach (var row in result.Rows)
        {
            builder.AppendLine($"{EscapeCsvField(row.ChuteCode)},{EscapeCsvField(row.WaveCode)},{row.RecirculatedCount}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// 执行 BuildSummaryAsync 方法。
    /// </summary>
    private async Task<RecirculationSummaryQueryResult> BuildSummaryAsync(
        RecirculationSummaryQueryRequest request,
        string? selectedChuteCode,
        string sortOrder,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 BuildSummaryAsync 方法的核心处理流程。
        var rows = await _businessTaskRepository.AggregateRecirculationSummaryAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            selectedChuteCode,
            cancellationToken);
        var orderedRows = (sortOrder == "Least" ? rows.OrderBy(row => row.RecirculatedCount) : rows.OrderByDescending(row => row.RecirculatedCount))
            .ThenBy(row => row.ChuteCode, StringComparer.Ordinal)
            .ThenBy(row => row.WaveCode, StringComparer.Ordinal)
            .Select(row => new RecirculationSummaryRow
            {
                ChuteCode = row.ChuteCode,
                WaveCode = row.WaveCode,
                RecirculatedCount = row.RecirculatedCount
            })
            .ToList();

        return new RecirculationSummaryQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            SelectedChuteCode = selectedChuteCode,
            SortOrder = sortOrder,
            Rows = orderedRows
        };
    }

    private static string NormalizeSortOrder(string? sortOrder)
    {
        return string.Equals(sortOrder, "Least", StringComparison.OrdinalIgnoreCase) ? "Least" : "Most";
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string EscapeCsvField(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

