using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Utilities;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 定义 SortingReportQueryService 类型。
/// </summary>
public sealed class SortingReportQueryService : ISortingReportQueryService
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

    public SortingReportQueryService(IBusinessTaskRepository businessTaskRepository)
        : this(businessTaskRepository, new MemoryCache(new MemoryCacheOptions()), new QueryCacheOptions())
    {
    }

    /// <summary>
    /// 执行 SortingReportQueryService 方法。
    /// </summary>
    public SortingReportQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        // 步骤：执行 SortingReportQueryService 方法的核心处理流程。
        _businessTaskRepository = businessTaskRepository;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    public async Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new SortingReportQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        var selectedDockCode = string.IsNullOrWhiteSpace(request.DockCode) ? null : request.DockCode.Trim();
        var cacheKey = $"sorting-report:{request.StartTimeLocal.ToString(CacheKeyDateTimeFormat)}:{request.EndTimeLocal.ToString(CacheKeyDateTimeFormat)}:{selectedDockCode ?? NullCacheValue}";
        if (_queryCacheOptions.Enabled)
        {
            var ttl = Math.Clamp(_queryCacheOptions.SortingReportSeconds, 1, 120);
            var cached = await MemoryCacheSingleFlight.GetOrCreateAsync(
                _memoryCache,
                cacheKey,
                TimeSpan.FromSeconds(ttl),
                _ => BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedDockCode, CancellationToken.None),
                cancellationToken);
            return cached ?? new SortingReportQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedDockCode, cancellationToken);
    }

    /// <summary>
    /// 执行 BuildResultAsync 方法。
    /// </summary>
    private async Task<SortingReportQueryResult> BuildResultAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? selectedDockCode,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 BuildResultAsync 方法的核心处理流程。
        var dockRows = await _businessTaskRepository.AggregateDockDashboardAsync(
            startTimeLocal,
            endTimeLocal,
            null,
            selectedDockCode,
            cancellationToken);
        var rows = dockRows
            .Select(row => new SortingReportRow
            {
                DockCode = row.DockCode,
                SplitTotalCount = row.SplitTotalCount,
                FullCaseTotalCount = row.FullCaseTotalCount,
                SplitSortedCount = row.SplitSortedCount,
                FullCaseSortedCount = row.FullCaseSortedCount,
                RecirculatedCount = row.RecirculatedCount,
                ExceptionCount = _queryPolicy.IsDockSeven(row.DockCode) ? row.ExceptionCount : 0
            })
            .OrderBy(row => row.DockCode, StringComparer.Ordinal)
            .ToList();

        return new SortingReportQueryResult
        {
            StartTimeLocal = startTimeLocal,
            EndTimeLocal = endTimeLocal,
            SelectedDockCode = selectedDockCode,
            Rows = rows
        };
    }

    public async Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await QueryAsync(request, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("码头号,拆零总数,整件总数,拆零分拣数,整件分拣数,回流数,异常数");
        foreach (var row in result.Rows)
        {
            builder.AppendLine($"{EscapeCsvField(row.DockCode)},{row.SplitTotalCount},{row.FullCaseTotalCount},{row.SplitSortedCount},{row.FullCaseSortedCount},{row.RecirculatedCount},{row.ExceptionCount}");
        }

        return builder.ToString();
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

