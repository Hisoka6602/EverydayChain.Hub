using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Application.Queries;

public sealed class DockDashboardQueryService : IDockDashboardQueryService
{
    private const string CacheKeyDateTimeFormat = "yyyyMMddHHmmssfffffff";
    private const string NullCacheValue = "(null)";

    private readonly IBusinessTaskRepository _businessTaskRepository;
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();
    private readonly IMemoryCache _memoryCache;
    private readonly QueryCacheOptions _queryCacheOptions;

    public DockDashboardQueryService(IBusinessTaskRepository businessTaskRepository)
        : this(businessTaskRepository, new MemoryCache(new MemoryCacheOptions()), new QueryCacheOptions())
    {
    }

    public DockDashboardQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
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
        if (!_queryCacheOptions.Enabled || string.IsNullOrWhiteSpace(selectedWaveCode))
        {
            return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedWaveCode, cancellationToken);
        }

        var cacheKey = $"dock-dashboard:{request.StartTimeLocal.ToString(CacheKeyDateTimeFormat)}:{request.EndTimeLocal.ToString(CacheKeyDateTimeFormat)}:{selectedWaveCode ?? NullCacheValue}";
        var ttl = Math.Clamp(_queryCacheOptions.DockDashboardSeconds, 1, 60);
        var cached = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl);
            return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedWaveCode, cancellationToken);
        });
        return cached ?? new DockDashboardQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal
        };
    }

    private async Task<DockDashboardQueryResult> BuildResultAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? selectedWaveCode,
        CancellationToken cancellationToken)
    {
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
