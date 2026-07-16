using System.Text;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Utilities;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 定义 WaveQueryService 类型。
/// </summary>
public sealed class WaveQueryService : IWaveQueryService
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
    /// 存储 SplitZone1Code 字段。
    /// </summary>
    private const string SplitZone1Code = "SplitZone1";
    /// <summary>
    /// 存储 SplitZone2Code 字段。
    /// </summary>
    private const string SplitZone2Code = "SplitZone2";
    /// <summary>
    /// 存储 SplitZone3Code 字段。
    /// </summary>
    private const string SplitZone3Code = "SplitZone3";
    /// <summary>
    /// 存储 SplitZone4Code 字段。
    /// </summary>
    private const string SplitZone4Code = "SplitZone4";
    /// <summary>
    /// 存储 FullCaseCode 字段。
    /// </summary>
    private const string FullCaseCode = "FullCase";

    private static readonly IReadOnlyList<string> ZoneOutputOrder =
    [
        SplitZone1Code,
        SplitZone2Code,
        SplitZone3Code,
        SplitZone4Code,
        FullCaseCode
    ];

    private static readonly IReadOnlyDictionary<string, string> SplitWorkingAreaZoneMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = SplitZone1Code,
            ["YA1"] = SplitZone1Code,
            ["A1"] = SplitZone1Code,
            ["2"] = SplitZone2Code,
            ["YA2"] = SplitZone2Code,
            ["A2"] = SplitZone2Code,
            ["3"] = SplitZone3Code,
            ["YB1"] = SplitZone3Code,
            ["B1"] = SplitZone3Code,
            ["4"] = SplitZone4Code,
            ["YB2"] = SplitZone4Code,
            ["B2"] = SplitZone4Code
        };

    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;
    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<WaveQueryService> _logger;
    /// <summary>
    /// 存储 _memoryCache 字段。
    /// </summary>
    private readonly IMemoryCache _memoryCache;
    /// <summary>
    /// 存储 _queryCacheOptions 字段。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;
    /// <summary>
    /// 统一处理波次查询的时间窗口与分页约束。
    /// </summary>
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();

    public WaveQueryService(IBusinessTaskRepository businessTaskRepository, ILogger<WaveQueryService> logger)
        : this(
            businessTaskRepository,
            logger,
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions())
    {
    }

    /// <summary>
    /// 执行 WaveQueryService 方法。
    /// </summary>
    public WaveQueryService(
        IBusinessTaskRepository businessTaskRepository,
        ILogger<WaveQueryService> logger,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        // 步骤：执行 WaveQueryService 方法的核心处理流程。
        _businessTaskRepository = businessTaskRepository;
        _logger = logger;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    public async Task<CurrentWaveQueryResult> QueryCurrentAsync(CurrentWaveQueryRequest request, CancellationToken cancellationToken)
    {
        var emptyResult = new CurrentWaveQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal
        };
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return emptyResult;
        }

        var normalizedStartTime = QueryCacheTimeBucket.Normalize(request.StartTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var normalizedEndTime = QueryCacheTimeBucket.Normalize(request.EndTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var cacheKey = $"wave-current:{normalizedStartTime.ToString(CacheKeyDateTimeFormat)}:{normalizedEndTime.ToString(CacheKeyDateTimeFormat)}";
        return await GetCachedAsync(
            cacheKey,
            _queryCacheOptions.CurrentWaveSeconds,
            () => BuildCurrentAsync(request, cancellationToken),
            emptyResult,
            cancellationToken);
    }

    public async Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken)
    {
        var emptyResult = new WaveOptionsQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal
        };
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return emptyResult;
        }

        var normalizedStartTime = QueryCacheTimeBucket.Normalize(request.StartTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var normalizedEndTime = QueryCacheTimeBucket.Normalize(request.EndTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var cacheKey = $"wave-options:{normalizedStartTime.ToString(CacheKeyDateTimeFormat)}:{normalizedEndTime.ToString(CacheKeyDateTimeFormat)}";
        return await GetCachedAsync(
            cacheKey,
            _queryCacheOptions.WaveOptionsSeconds,
            () => BuildOptionsAsync(request, cancellationToken),
            emptyResult,
            cancellationToken);
    }

    public async Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.EndTimeLocal <= request.StartTimeLocal || string.IsNullOrWhiteSpace(request.WaveCode))
        {
            return null;
        }

        var normalizedWaveCode = request.WaveCode.Trim();
        var normalizedStartTime = QueryCacheTimeBucket.Normalize(request.StartTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var normalizedEndTime = QueryCacheTimeBucket.Normalize(request.EndTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var cacheKey = $"wave-summary:{normalizedStartTime.ToString(CacheKeyDateTimeFormat)}:{normalizedEndTime.ToString(CacheKeyDateTimeFormat)}:{normalizedWaveCode}";
        return await GetCachedAsync<WaveSummaryQueryResult?>(
            cacheKey,
            _queryCacheOptions.WaveSummarySeconds,
            () => BuildSummaryAsync(request, normalizedWaveCode, cancellationToken),
            null,
            cancellationToken);
    }

    public async Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.EndTimeLocal <= request.StartTimeLocal || string.IsNullOrWhiteSpace(request.WaveCode))
        {
            return null;
        }

        var normalizedWaveCode = request.WaveCode.Trim();
        var normalizedStartTime = QueryCacheTimeBucket.Normalize(request.StartTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var normalizedEndTime = QueryCacheTimeBucket.Normalize(request.EndTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var cacheKey = $"wave-zones:{normalizedStartTime.ToString(CacheKeyDateTimeFormat)}:{normalizedEndTime.ToString(CacheKeyDateTimeFormat)}:{normalizedWaveCode}";
        return await GetCachedAsync<WaveZoneQueryResult?>(
            cacheKey,
            _queryCacheOptions.WaveZoneSeconds,
            () => BuildZonesAsync(request, normalizedWaveCode, cancellationToken),
            null,
            cancellationToken);
    }

    public async Task<string> ExportZonesCsvAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await QueryZonesAsync(request, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("区域名称,总数,待分拣数,进度百分比,回流数,异常数");
        if (result is null)
        {
            return builder.ToString();
        }

        foreach (var zone in result.Zones)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsvField(zone.ZoneName),
                zone.TotalCount,
                zone.UnsortedCount,
                zone.SortedProgressPercent,
                zone.RecirculatedCount,
                zone.ExceptionCount));
        }

        return builder.ToString();
    }

    public async Task<WaveListQueryResult> QueryListAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
    {
        var emptyResult = new WaveListQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal
        };
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return emptyResult;
        }

        var normalizedStartTime = QueryCacheTimeBucket.Normalize(request.StartTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var normalizedEndTime = QueryCacheTimeBucket.Normalize(request.EndTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var cacheKey = $"wave-list:{normalizedStartTime.ToString(CacheKeyDateTimeFormat)}:{normalizedEndTime.ToString(CacheKeyDateTimeFormat)}";
        return await GetCachedAsync(
            cacheKey,
            _queryCacheOptions.WaveListSeconds,
            () => BuildListAsync(request, cancellationToken),
            emptyResult,
            cancellationToken);
    }

    public async Task<string> ExportListCsvAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await QueryListAsync(request, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("波次号,备注,包裹总数,待分拣数,拆零总数,整件总数,拆零未分拣数量,整件未分拣数量,回流数,异常数,创建时间,状态");
        foreach (var item in result.Items)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsvField(item.WaveCode),
                EscapeCsvField(item.WaveRemark ?? string.Empty),
                item.PackageTotal,
                item.UnsortedCount,
                item.SplitTotal,
                item.FullCaseTotal,
                item.SplitUnsortedCount,
                item.FullCaseUnsortedCount,
                item.RecirculatedCount,
                item.ExceptionCount,
                item.CreatedTimeLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                EscapeCsvField(item.Status)));
        }

        return builder.ToString();
    }

    public async Task<WaveCleanupQueryResult> QueryCleanupWaveAsync(string? waveCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(waveCode))
        {
            return await GetCachedAsync(
                "wave-cleanup:all",
                _queryCacheOptions.WaveCleanupSeconds,
                () => BuildCleanableWavesAsync(cancellationToken),
                new WaveCleanupQueryResult(),
                cancellationToken);
        }

        var normalizedWaveCode = waveCode.Trim();
        var cacheKey = $"wave-cleanup:{normalizedWaveCode}";
        return await GetCachedAsync(
            cacheKey,
            _queryCacheOptions.WaveCleanupSeconds,
            () => BuildCleanupWaveAsync(normalizedWaveCode, cancellationToken),
            new WaveCleanupQueryResult(),
            cancellationToken);
    }

    public async Task<WaveDetailQueryResult> QueryDetailsAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken)
    {
        var normalizedWaveCode = request.WaveCode.Trim();
        var emptyResult = new WaveDetailQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            WaveCode = normalizedWaveCode
        };
        if (request.EndTimeLocal <= request.StartTimeLocal || string.IsNullOrWhiteSpace(request.WaveCode))
        {
            return emptyResult;
        }

        var normalizedStartTime = QueryCacheTimeBucket.Normalize(request.StartTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var normalizedEndTime = QueryCacheTimeBucket.Normalize(request.EndTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var cacheKey = $"wave-details:{normalizedStartTime.ToString(CacheKeyDateTimeFormat)}:{normalizedEndTime.ToString(CacheKeyDateTimeFormat)}:{normalizedWaveCode}";
        return await GetCachedAsync(
            cacheKey,
            _queryCacheOptions.WaveDetailSeconds,
            () => BuildDetailsAsync(request, normalizedWaveCode, cancellationToken),
            emptyResult,
            cancellationToken);
    }

    private async Task<WaveDetailQueryResult> BuildDetailsAsync(
        WaveDetailQueryRequest request,
        string normalizedWaveCode,
        CancellationToken cancellationToken)
    {
        var result = new WaveDetailQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            WaveCode = normalizedWaveCode
        };

        var tasks = await _businessTaskRepository.FindByWaveCodeAndCreatedTimeRangeAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            normalizedWaveCode,
            cancellationToken);

        result.WaveRemark = tasks
            .Where(task => !string.IsNullOrWhiteSpace(task.WaveRemark))
            .OrderByDescending(task => task.UpdatedTimeLocal)
            .Select(task => task.WaveRemark!.Trim())
            .FirstOrDefault();

        result.Items = tasks
            .OrderByDescending(task => task.ScannedAtLocal ?? task.CreatedTimeLocal)
            .ThenByDescending(task => task.UpdatedTimeLocal)
            .ThenByDescending(task => task.Id)
            .Select(task => new WaveDetailItem
            {
                TaskCode = task.TaskCode,
                WaveCode = NormalizeOptionalText(task.WaveCode) ?? task.WaveCode ?? string.Empty,
                WaveRemark = NormalizeOptionalText(task.WaveRemark),
                SourceType = ChineseDisplayText.ForSourceType(task.SourceType),
                WorkingArea = NormalizeOptionalText(task.WorkingArea),
                Barcode = NormalizeOptionalText(task.Barcode),
                OrderId = NormalizeOptionalText(task.OrderId),
                StoreId = NormalizeOptionalText(task.StoreId),
                StoreName = NormalizeOptionalText(task.StoreName),
                ProductCode = NormalizeOptionalText(task.ProductCode),
                PickLocation = NormalizeOptionalText(task.PickLocation),
                ChuteCode = ResolveChuteCode(task),
                Status = ChineseDisplayText.ForTaskStatus(task.Status),
                IsRecirculated = task.IsRecirculated || _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode),
                IsException = task.IsException || task.Status == BusinessTaskStatus.Exception,
                ScannedAtLocal = task.ScannedAtLocal,
                CreatedTimeLocal = task.CreatedTimeLocal,
                UpdatedTimeLocal = task.UpdatedTimeLocal
            })
            .ToList();
        return result;
    }

    public async Task<string> ExportDetailsCsvAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await QueryDetailsAsync(request, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("任务编码,波次号,波次备注,来源类型,作业区域,条码,订单号,门店号,门店名称,商品编码,拣货位,格口,状态,是否回流,是否异常,扫描时间,创建时间,更新时间");
        foreach (var item in result.Items)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsvField(item.TaskCode),
                EscapeCsvField(item.WaveCode),
                EscapeCsvField(item.WaveRemark ?? string.Empty),
                EscapeCsvField(item.SourceType),
                EscapeCsvField(item.WorkingArea ?? string.Empty),
                EscapeCsvField(item.Barcode ?? string.Empty),
                EscapeCsvField(item.OrderId ?? string.Empty),
                EscapeCsvField(item.StoreId ?? string.Empty),
                EscapeCsvField(item.StoreName ?? string.Empty),
                EscapeCsvField(item.ProductCode ?? string.Empty),
                EscapeCsvField(item.PickLocation ?? string.Empty),
                EscapeCsvField(item.ChuteCode ?? string.Empty),
                EscapeCsvField(item.Status),
                ChineseDisplayText.YesNo(item.IsRecirculated),
                ChineseDisplayText.YesNo(item.IsException),
                item.ScannedAtLocal?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
                item.CreatedTimeLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                item.UpdatedTimeLocal.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        return builder.ToString();
    }

    private async Task<CurrentWaveQueryResult> BuildCurrentAsync(CurrentWaveQueryRequest request, CancellationToken cancellationToken)
    {
        var result = new CurrentWaveQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal
        };
        var latestTask = await _businessTaskRepository.FindLatestScannedWithWaveByCreatedTimeRangeAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            cancellationToken);
        if (latestTask is null)
        {
            return result;
        }

        result.WaveCode = NormalizeOptionalText(latestTask.WaveCode);
        result.WaveRemark = NormalizeOptionalText(latestTask.WaveRemark);
        result.Barcode = NormalizeOptionalText(latestTask.Barcode);
        result.ScanTimeLocal = latestTask.ScannedAtLocal;
        return result;
    }

    private async Task<WaveOptionsQueryResult> BuildOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken)
    {
        var waveRows = await _businessTaskRepository.ListWaveOptionsByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
        return new WaveOptionsQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            WaveOptions = waveRows
                .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
                .Select(row => new WaveOptionItem
                {
                    WaveCode = row.WaveCode,
                    WaveRemark = row.WaveRemark
                })
                .ToList()
        };
    }

    /// <summary>
    /// 执行 BuildSummaryAsync 方法。
    /// </summary>
    private async Task<WaveSummaryQueryResult?> BuildSummaryAsync(
        WaveSummaryQueryRequest request,
        string normalizedWaveCode,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 BuildSummaryAsync 方法的核心处理流程。
        var taskStats = await FindWaveTaskStatsAsync(request.StartTimeLocal, request.EndTimeLocal, normalizedWaveCode, cancellationToken);
        if (taskStats.Count == 0)
        {
            return null;
        }

        var totalCount = taskStats.Count;
        var unsortedCount = taskStats.Count(task => !IsSorted(task.Status));
        var sortedCount = totalCount - unsortedCount;
        return new WaveSummaryQueryResult
        {
            WaveCode = normalizedWaveCode,
            WaveRemark = ResolveWaveRemark(taskStats),
            TotalCount = totalCount,
            UnsortedCount = unsortedCount,
            SortedProgressPercent = _queryPolicy.CalculatePercent(sortedCount, totalCount),
            RecirculatedCount = taskStats.Count(task => _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode)),
            ExceptionCount = taskStats.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception)
        };
    }

    /// <summary>
    /// 执行 BuildZonesAsync 方法。
    /// </summary>
    private async Task<WaveZoneQueryResult?> BuildZonesAsync(
        WaveZoneQueryRequest request,
        string normalizedWaveCode,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 BuildZonesAsync 方法的核心处理流程。
        var taskStats = await FindWaveTaskStatsAsync(request.StartTimeLocal, request.EndTimeLocal, normalizedWaveCode, cancellationToken);
        if (taskStats.Count == 0)
        {
            return null;
        }

        var zoneMap = BuildZoneAccumulatorMap();
        foreach (var task in taskStats)
        {
            var targetZone = ResolveTargetZone(task);
            if (targetZone is null || !zoneMap.TryGetValue(targetZone, out var zone))
            {
                continue;
            }

            zone.TotalCount++;
            if (!IsSorted(task.Status))
            {
                zone.UnsortedCount++;
            }

            if (_queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode))
            {
                zone.RecirculatedCount++;
            }

            if (task.IsException || task.Status == BusinessTaskStatus.Exception)
            {
                zone.ExceptionCount++;
            }
        }

        var zoneSummaries = ZoneOutputOrder
            .Select(zoneCode => zoneMap.TryGetValue(zoneCode, out var zone) ? zone : null)
            .Where(zone => zone is not null)
            .Select(zone => zone!)
            .Select(zone =>
            {
                var sortedCount = zone.TotalCount - zone.UnsortedCount;
                return new WaveZoneSummary
                {
                    ZoneCode = zone.ZoneCode,
                    ZoneName = zone.ZoneName,
                    TotalCount = zone.TotalCount,
                    UnsortedCount = zone.UnsortedCount,
                    SortedProgressPercent = _queryPolicy.CalculatePercent(sortedCount, zone.TotalCount),
                    RecirculatedCount = zone.RecirculatedCount,
                    ExceptionCount = zone.ExceptionCount
                };
            })
            .ToList();

        return new WaveZoneQueryResult
        {
            WaveCode = normalizedWaveCode,
            WaveRemark = ResolveWaveRemark(taskStats),
            Zones = zoneSummaries
        };
    }

    private async Task<WaveListQueryResult> BuildListAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
    {
        var waveRows = await _businessTaskRepository.AggregateWaveDashboardAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
        var optionMap = (await _businessTaskRepository.ListWaveOptionsByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken))
            .ToDictionary(item => item.WaveCode, item => item.WaveRemark, StringComparer.OrdinalIgnoreCase);

        var items = waveRows
            .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
            .Select(row => BuildWaveListItem(
                row,
                optionMap.TryGetValue(row.WaveCode, out var waveRemark) ? waveRemark : null))
            .ToList();

        return new WaveListQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            Items = items
        };
    }

    private async Task<WaveCleanupQueryResult> BuildCleanupWaveAsync(string normalizedWaveCode, CancellationToken cancellationToken)
    {
        var tasks = await _businessTaskRepository.FindByWaveCodeAsync(normalizedWaveCode, cancellationToken);
        if (tasks.Count == 0)
        {
            return new WaveCleanupQueryResult();
        }

        var aggregate = new BusinessTaskWaveAggregateRow
        {
            WaveCode = normalizedWaveCode,
            TotalCount = tasks.Count,
            UnsortedCount = tasks.Count(task => !IsSorted(task.Status)),
            FullCaseTotalCount = tasks.Count(task => task.SourceType == BusinessTaskSourceType.FullCase),
            FullCaseUnsortedCount = tasks.Count(task => task.SourceType == BusinessTaskSourceType.FullCase && !IsSorted(task.Status)),
            SplitTotalCount = tasks.Count(task => task.SourceType == BusinessTaskSourceType.Split),
            SplitUnsortedCount = tasks.Count(task => task.SourceType == BusinessTaskSourceType.Split && !IsSorted(task.Status)),
            RecognitionCount = tasks.Count(task => task.ScannedAtLocal.HasValue),
            RecirculatedCount = tasks.Count(task => task.IsRecirculated || _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode)),
            ExceptionCount = tasks.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception),
            TotalVolumeMm3 = tasks.Sum(task => task.VolumeMm3 ?? 0M),
            TotalWeightGram = tasks.Sum(task => task.WeightGram ?? 0M),
            EarliestCreatedTimeLocal = tasks.Min(task => task.CreatedTimeLocal)
        };

        return new WaveCleanupQueryResult
        {
            Items =
            [
                new WaveCleanupWaveItem
                {
                    WaveCode = aggregate.WaveCode,
                    WaveRemark = tasks
                        .Where(task => !string.IsNullOrWhiteSpace(task.WaveRemark))
                        .OrderByDescending(task => task.UpdatedTimeLocal)
                        .Select(task => task.WaveRemark!.Trim())
                        .FirstOrDefault(),
                    PackageTotal = aggregate.TotalCount,
                    SplitTotal = aggregate.SplitTotalCount,
                    FullCaseTotal = aggregate.FullCaseTotalCount,
                    CreatedTimeLocal = aggregate.EarliestCreatedTimeLocal,
                    Status = ResolveWaveStatus(aggregate)
                }
            ]
        };
    }

    /// <summary>
    /// 执行当前业务方法。
    /// </summary>
    private async Task<T> GetCachedAsync<T>(
        string cacheKey,
        int ttlSeconds,
        Func<Task<T>> factory,
        T fallback,
        CancellationToken cancellationToken)
    {
        // 步骤：执行当前业务方法的核心处理流程。
        if (!_queryCacheOptions.Enabled)
        {
            return await factory();
        }

        var ttl = Math.Clamp(ttlSeconds, 1, 60);
        var cached = await MemoryCacheSingleFlight.GetOrCreateAsync(
            _memoryCache,
            cacheKey,
            TimeSpan.FromSeconds(ttl),
            _ => factory(),
            cancellationToken);
        return cached is null ? fallback : cached;
    }

    /// <summary>
    /// 执行 FindWaveTaskStatsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<BusinessTaskWaveTaskStatsRow>> FindWaveTaskStatsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string waveCode,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 FindWaveTaskStatsAsync 方法的核心处理流程。
        if (endTimeLocal <= startTimeLocal || string.IsNullOrWhiteSpace(waveCode))
        {
            return [];
        }

        return await _businessTaskRepository.ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync(startTimeLocal, endTimeLocal, waveCode.Trim(), cancellationToken);
    }

    private static string? ResolveWaveRemark(IReadOnlyList<BusinessTaskWaveTaskStatsRow> tasks)
    {
        return tasks
            .Where(task => !string.IsNullOrWhiteSpace(task.WaveRemark))
            .OrderByDescending(task => task.UpdatedTimeLocal)
            .Select(task => task.WaveRemark!.Trim())
            .FirstOrDefault();
    }

    private static bool IsSorted(BusinessTaskStatus status)
    {
        return status == BusinessTaskStatus.Dropped || status == BusinessTaskStatus.FeedbackPending;
    }

    private static string? ResolveChuteCode(BusinessTaskEntity task)
    {
        if (!string.IsNullOrWhiteSpace(task.ActualChuteCode))
        {
            return task.ActualChuteCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(task.TargetChuteCode))
        {
            return task.TargetChuteCode.Trim();
        }

        return string.IsNullOrWhiteSpace(task.ResolvedDockCode) ? null : task.ResolvedDockCode.Trim();
    }

    private static string ResolveWaveStatus(BusinessTaskWaveAggregateRow row)
    {
        if (row.ExceptionCount > 0)
        {
            return "异常待处理";
        }

        if (row.UnsortedCount > 0)
        {
            return "分拣中";
        }

        return "已完成";
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private WaveListItem BuildWaveListItem(BusinessTaskWaveAggregateRow row, string? waveRemark)
    {
        return new WaveListItem
        {
            WaveCode = row.WaveCode,
            WaveRemark = waveRemark,
            PackageTotal = row.TotalCount,
            UnsortedCount = row.UnsortedCount,
            SplitTotal = row.SplitTotalCount,
            FullCaseTotal = row.FullCaseTotalCount,
            SplitUnsortedCount = row.SplitUnsortedCount,
            FullCaseUnsortedCount = row.FullCaseUnsortedCount,
            RecirculatedCount = row.RecirculatedCount,
            ExceptionCount = row.ExceptionCount,
            CreatedTimeLocal = row.EarliestCreatedTimeLocal,
            Status = ResolveWaveStatus(row)
        };
    }

    private static Dictionary<string, ZoneAccumulator> BuildZoneAccumulatorMap()
    {
        return new Dictionary<string, ZoneAccumulator>(StringComparer.Ordinal)
        {
            [SplitZone1Code] = new ZoneAccumulator(SplitZone1Code, "拆零区1"),
            [SplitZone2Code] = new ZoneAccumulator(SplitZone2Code, "拆零区2"),
            [SplitZone3Code] = new ZoneAccumulator(SplitZone3Code, "拆零区3"),
            [SplitZone4Code] = new ZoneAccumulator(SplitZone4Code, "拆零区4"),
            [FullCaseCode] = new ZoneAccumulator(FullCaseCode, "整件区")
        };
    }

    private string? ResolveTargetZone(BusinessTaskWaveTaskStatsRow task)
    {
        if (task.SourceType == BusinessTaskSourceType.FullCase)
        {
            return FullCaseCode;
        }

        if (task.SourceType != BusinessTaskSourceType.Split)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(task.WorkingArea))
        {
            _logger.LogWarning(
                "Wave zone statistics skipped a split task because WorkingArea is empty. TaskCode={TaskCode}, WaveCode={WaveCode}, SourceType={SourceType}",
                task.TaskCode,
                task.WaveCode,
                task.SourceType);
            return null;
        }

        var normalizedWorkingArea = NormalizeWorkingAreaKey(task.WorkingArea);
        if (SplitWorkingAreaZoneMap.TryGetValue(normalizedWorkingArea, out var zoneCode))
        {
            return zoneCode;
        }

        return LogAndSkipInvalidWorkingArea(task);
    }

    private async Task<WaveCleanupQueryResult> BuildCleanableWavesAsync(CancellationToken cancellationToken)
    {
        var rows = await _businessTaskRepository.AggregateCleanableWavesAsync(cancellationToken);
        return new WaveCleanupQueryResult
        {
            Items = rows
                .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
                .Select(row => new WaveCleanupWaveItem
                {
                    WaveCode = row.WaveCode,
                    WaveRemark = row.WaveRemark,
                    PackageTotal = row.TotalCount,
                    SplitTotal = row.SplitTotalCount,
                    FullCaseTotal = row.FullCaseTotalCount,
                    CreatedTimeLocal = row.EarliestCreatedTimeLocal,
                    Status = ResolveWaveStatus(row)
                })
                .ToList()
        };
    }

    private string? LogAndSkipInvalidWorkingArea(BusinessTaskWaveTaskStatsRow task)
    {
        _logger.LogWarning(
            "Wave zone statistics skipped a split task because WorkingArea is invalid. TaskCode={TaskCode}, WaveCode={WaveCode}, SourceType={SourceType}, WorkingArea={WorkingArea}",
            task.TaskCode,
            task.WaveCode,
            task.SourceType,
            task.WorkingArea);
        return null;
    }

    private static string NormalizeWorkingAreaKey(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
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

    /// <summary>
    /// 定义 ZoneAccumulator 类型。
    /// </summary>
    private sealed class ZoneAccumulator(string zoneCode, string zoneName)
    {
        /// <summary>
        /// 获取或设置 ZoneCode。
        /// </summary>
        public string ZoneCode { get; } = zoneCode;

        /// <summary>
        /// 获取或设置 ZoneName。
        /// </summary>
        public string ZoneName { get; } = zoneName;

        /// <summary>
        /// 获取或设置 TotalCount。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 获取或设置 UnsortedCount。
        /// </summary>
        public int UnsortedCount { get; set; }

        /// <summary>
        /// 获取或设置 RecirculatedCount。
        /// </summary>
        public int RecirculatedCount { get; set; }

        /// <summary>
        /// 获取或设置 ExceptionCount。
        /// </summary>
        public int ExceptionCount { get; set; }
    }
}

