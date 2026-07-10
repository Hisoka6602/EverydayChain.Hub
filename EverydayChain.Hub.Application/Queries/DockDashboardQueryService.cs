using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Utilities;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 定义 DockDashboardQueryService 类型。
/// </summary>
public sealed class DockDashboardQueryService : IDockDashboardQueryService
{
    /// <summary>
    /// 存储 CacheKeyDateTimeFormat 字段。
    /// </summary>
    private const string CacheKeyDateTimeFormat = "yyyyMMddHHmmssfffffff";
    private const string NullCacheValue = "(null)";

    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();
    /// <summary>
    /// 存储 _memoryCache 字段。
    /// </summary>
    private readonly IMemoryCache _memoryCache;
    /// <summary>
    /// 存储 _queryCacheOptions 字段。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;

    public DockDashboardQueryService(IBusinessTaskRepository businessTaskRepository)
        : this(businessTaskRepository, new MemoryCache(new MemoryCacheOptions()), new QueryCacheOptions())
    {
    }

    /// <summary>
    /// 执行 DockDashboardQueryService 方法。
    /// </summary>
    public DockDashboardQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        // 步骤：执行 DockDashboardQueryService 方法的核心处理流程。
        _businessTaskRepository = businessTaskRepository;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    public async Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new DockDashboardQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        var selectedWaveCode = NormalizeOptionalText(request.WaveCode);
        if (!_queryCacheOptions.Enabled)
        {
            return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedWaveCode, cancellationToken);
        }

        var normalizedStartTime = QueryCacheTimeBucket.Normalize(request.StartTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var normalizedEndTime = QueryCacheTimeBucket.Normalize(request.EndTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var cacheKey = $"dock-dashboard:{normalizedStartTime.ToString(CacheKeyDateTimeFormat)}:{normalizedEndTime.ToString(CacheKeyDateTimeFormat)}:{selectedWaveCode ?? NullCacheValue}";
        var ttl = Math.Clamp(_queryCacheOptions.DockDashboardSeconds, 1, 60);
        var cached = await MemoryCacheSingleFlight.GetOrCreateAsync(
            _memoryCache,
            cacheKey,
            TimeSpan.FromSeconds(ttl),
            _ => BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedWaveCode, CancellationToken.None),
            cancellationToken);
        return cached ?? new DockDashboardQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal
        };
    }

    /// <summary>
    /// 执行 BuildResultAsync 方法。
    /// </summary>
    private async Task<DockDashboardQueryResult> BuildResultAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? selectedWaveCode,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 BuildResultAsync 方法的核心处理流程。
        var waveOptions = await _businessTaskRepository.ListWaveCodesByCreatedTimeRangeAsync(startTimeLocal, endTimeLocal, cancellationToken);
        var effectiveSelectedWaveCode = selectedWaveCode;
        if (string.IsNullOrWhiteSpace(effectiveSelectedWaveCode))
        {
            var latestTask = await _businessTaskRepository.FindLatestScannedWithWaveByCreatedTimeRangeAsync(startTimeLocal, endTimeLocal, cancellationToken);
            var autoWaveCode = NormalizeOptionalText(latestTask?.WaveCode);
            if (!string.IsNullOrWhiteSpace(autoWaveCode)
                && waveOptions.Contains(autoWaveCode, StringComparer.OrdinalIgnoreCase))
            {
                effectiveSelectedWaveCode = autoWaveCode;
            }
        }

        var dockRows = await _businessTaskRepository.AggregateDockDashboardAsync(
            startTimeLocal,
            endTimeLocal,
            effectiveSelectedWaveCode,
            null,
            cancellationToken);
        var summaries = dockRows
            .Select(row => new DockDashboardSummary
            {
                DockCode = row.DockCode,
                SplitUnsortedCount = row.SplitUnsortedCount,
                FullCaseUnsortedCount = row.FullCaseUnsortedCount,
                RecirculatedCount = row.RecirculatedCount,
                ExceptionCount = _queryPolicy.IsDockSeven(row.DockCode) ? row.ExceptionCount : 0,
                SortedCount = row.SortedCount,
                SortedProgressPercent = _queryPolicy.CalculatePercent(row.SortedCount, row.TotalCount)
            })
            .OrderBy(summary => summary.DockCode, StringComparer.Ordinal)
            .ToList();

        return new DockDashboardQueryResult
        {
            StartTimeLocal = startTimeLocal,
            EndTimeLocal = endTimeLocal,
            SelectedWaveCode = effectiveSelectedWaveCode,
            WaveOptions = waveOptions,
            DockSummaries = summaries
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

