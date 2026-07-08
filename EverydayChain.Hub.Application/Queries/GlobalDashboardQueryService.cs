using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Caching.Memory;
using EverydayChain.Hub.Application.Utilities;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 定义 GlobalDashboardQueryService 类型。
/// </summary>
public sealed class GlobalDashboardQueryService : IGlobalDashboardQueryService
{
    /// <summary>
    /// 存储 CacheKeyDateTimeFormat 字段。
    /// </summary>
    private const string CacheKeyDateTimeFormat = "yyyyMMddHHmmssfffffff";

    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;
    /// <summary>
    /// 存储 _scanLogRepository 字段。
    /// </summary>
    private readonly IScanLogRepository _scanLogRepository;
    /// <summary>
    /// 存储 _syncBatchRepository 字段。
    /// </summary>
    private readonly ISyncBatchRepository _syncBatchRepository;
    /// <summary>
    /// 存储 _syncTaskConfigRepository 字段。
    /// </summary>
    private readonly ISyncTaskConfigRepository _syncTaskConfigRepository;
    /// <summary>
    /// 存储 _memoryCache 字段。
    /// </summary>
    private readonly IMemoryCache _memoryCache;
    /// <summary>
    /// 存储 _queryCacheOptions 字段。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;

    public GlobalDashboardQueryService(IBusinessTaskRepository businessTaskRepository, IScanLogRepository scanLogRepository)
        : this(
            businessTaskRepository,
            scanLogRepository,
            new EmptySyncBatchRepository(),
            new EmptySyncTaskConfigRepository(),
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions())
    {
    }

    /// <summary>
    /// 执行 GlobalDashboardQueryService 方法。
    /// </summary>
    public GlobalDashboardQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IScanLogRepository scanLogRepository,
        ISyncBatchRepository syncBatchRepository,
        ISyncTaskConfigRepository syncTaskConfigRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        // 步骤：执行 EmptySyncBatchRepository 方法的核心处理流程。
        _businessTaskRepository = businessTaskRepository;
        _scanLogRepository = scanLogRepository;
        _syncBatchRepository = syncBatchRepository;
        _syncTaskConfigRepository = syncTaskConfigRepository;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    public async Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new GlobalDashboardQueryResult();
        }

        var cacheKey = $"global-dashboard:{request.StartTimeLocal.ToString(CacheKeyDateTimeFormat)}:{request.EndTimeLocal.ToString(CacheKeyDateTimeFormat)}";
        if (_queryCacheOptions.Enabled)
        {
            var ttl = Math.Clamp(_queryCacheOptions.GlobalDashboardSeconds, 1, 60);
            var cachedResult = await MemoryCacheSingleFlight.GetOrCreateAsync(
                _memoryCache,
                cacheKey,
                TimeSpan.FromSeconds(ttl),
                _ => BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, CancellationToken.None),
                cancellationToken);
            return cachedResult ?? new GlobalDashboardQueryResult();
        }

        return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
    }

    /// <summary>
    /// 执行 BuildResultAsync 方法。
    /// </summary>
    private async Task<GlobalDashboardQueryResult> BuildResultAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 BuildResultAsync 方法的核心处理流程。
        var waveRows = await _businessTaskRepository.AggregateWaveDashboardAsync(startTimeLocal, endTimeLocal, cancellationToken);
        var recognitionAggregate = await _scanLogRepository.AggregateRecognitionAsync(startTimeLocal, endTimeLocal, cancellationToken);
        var feedbackAggregate = await _businessTaskRepository.AggregateFeedbackAsync(startTimeLocal, endTimeLocal, cancellationToken);
        var enabledDefinitions = await _syncTaskConfigRepository.ListEnabledAsync(cancellationToken);
        var latestBatches = await _syncBatchRepository.ListLatestByTableCodesAsync(
            enabledDefinitions.Select(definition => definition.TableCode).ToArray(),
            cancellationToken);

        var totalCount = waveRows.Sum(row => row.TotalCount);
        var unsortedCount = waveRows.Sum(row => row.UnsortedCount);
        var fullCaseTotalCount = waveRows.Sum(row => row.FullCaseTotalCount);
        var fullCaseUnsortedCount = waveRows.Sum(row => row.FullCaseUnsortedCount);
        var splitTotalCount = waveRows.Sum(row => row.SplitTotalCount);
        var splitUnsortedCount = waveRows.Sum(row => row.SplitUnsortedCount);

        return new GlobalDashboardQueryResult
        {
            TotalCount = totalCount,
            UnsortedCount = unsortedCount,
            TotalSortedProgressPercent = CalculateProgressPercent(totalCount, unsortedCount),
            FullCaseTotalCount = fullCaseTotalCount,
            FullCaseUnsortedCount = fullCaseUnsortedCount,
            FullCaseSortedProgressPercent = CalculateProgressPercent(fullCaseTotalCount, fullCaseUnsortedCount),
            SplitTotalCount = splitTotalCount,
            SplitUnsortedCount = splitUnsortedCount,
            SplitSortedProgressPercent = CalculateProgressPercent(splitTotalCount, splitUnsortedCount),
            RecognitionRatePercent = CalculateRatePercent(recognitionAggregate.MatchedScanCount, recognitionAggregate.TotalScanCount),
            RecirculatedCount = waveRows.Sum(row => row.RecirculatedCount),
            ExceptionCount = waveRows.Sum(row => row.ExceptionCount),
            TotalVolumeMm3 = waveRows.Sum(row => row.TotalVolumeMm3),
            TotalWeightGram = waveRows.Sum(row => row.TotalWeightGram),
            LatestSyncTimeLocal = latestBatches
                .Select(ResolveBatchReferenceTime)
                .Where(value => value.HasValue)
                .OrderByDescending(value => value)
                .FirstOrDefault(),
            DataDownloadProgressPercent = enabledDefinitions.Count == 0
                ? 0M
                : CalculateRatePercent(
                    latestBatches.Count(batch => batch.Status == SyncBatchStatus.Completed),
                    enabledDefinitions.Count),
            DataWritebackProgressPercent = CalculateRatePercent(
                feedbackAggregate.CompletedFeedbackCount,
                feedbackAggregate.RequiredFeedbackCount),
            WaveSummaries = BuildWaveSummaries(waveRows)
        };
    }

    private static IReadOnlyList<WaveDashboardSummary> BuildWaveSummaries(IReadOnlyList<BusinessTaskWaveAggregateRow> waveRows)
    {
        return waveRows
            .Select(row => new WaveDashboardSummary
            {
                WaveCode = row.WaveCode,
                TotalCount = row.TotalCount,
                UnsortedCount = row.UnsortedCount,
                SortedProgressPercent = CalculateProgressPercent(row.TotalCount, row.UnsortedCount)
            })
            .OrderBy(summary => summary.WaveCode, StringComparer.Ordinal)
            .ToList();
    }

    private static DateTime? ResolveBatchReferenceTime(SyncBatch batch)
    {
        return batch.CompletedTimeLocal
            ?? batch.StartedTimeLocal
            ?? (batch.WindowEndLocal == default ? null : batch.WindowEndLocal);
    }

    private static decimal CalculateProgressPercent(int totalCount, int unsortedCount)
    {
        if (totalCount <= 0)
        {
            return 0M;
        }

        var sortedCount = totalCount - unsortedCount;
        return Math.Round((decimal)sortedCount * 100M / totalCount, 3, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateRatePercent(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0M;
        }

        return Math.Round((decimal)numerator * 100M / denominator, 3, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 定义 EmptySyncBatchRepository 类型。
    /// </summary>
    private sealed class EmptySyncBatchRepository : ISyncBatchRepository
    {
        /// <summary>
        /// 执行 CreateBatchAsync 方法。
        /// </summary>
        public Task CreateBatchAsync(SyncBatch batch, CancellationToken ct) => Task.CompletedTask;

        /// <summary>
        /// 执行 MarkInProgressAsync 方法。
        /// </summary>
        public Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct) => Task.CompletedTask;

        /// <summary>
        /// 执行 CompleteBatchAsync 方法。
        /// </summary>
        public Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct) => Task.CompletedTask;

        /// <summary>
        /// 执行 FailBatchAsync 方法。
        /// </summary>
        public Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct) => Task.CompletedTask;

        public Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct) => Task.FromResult<string?>(null);

        public Task<IReadOnlyList<SyncBatch>> ListLatestByTableCodesAsync(IReadOnlyCollection<string> tableCodes, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<SyncBatch>>([]);
        }
    }

    /// <summary>
    /// 定义 EmptySyncTaskConfigRepository 类型。
    /// </summary>
    private sealed class EmptySyncTaskConfigRepository : ISyncTaskConfigRepository
    {
        public Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct)
        {
            throw new InvalidOperationException("Empty sync config repository does not support direct table lookup.");
        }

        public Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<SyncTableDefinition>>([]);
        }

        public Task<int> GetMaxParallelTablesAsync(CancellationToken ct)
        {
            return Task.FromResult(1);
        }
    }
}


