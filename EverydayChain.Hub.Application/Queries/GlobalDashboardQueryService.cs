using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;
namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 总看板查询服务实现。
/// </summary>
public sealed class GlobalDashboardQueryService : IGlobalDashboardQueryService
{
    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 内存缓存。
    /// </summary>
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// 查询缓存配置。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;

    /// <summary>
    /// 初始化总看板查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    public GlobalDashboardQueryService(IBusinessTaskRepository businessTaskRepository)
        : this(businessTaskRepository, new MemoryCache(new MemoryCacheOptions()), new QueryCacheOptions())
    {
    }

    /// <summary>
    /// 初始化总看板查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="memoryCache">内存缓存。</param>
    /// <param name="queryCacheOptions">缓存配置。</param>
    public GlobalDashboardQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        _businessTaskRepository = businessTaskRepository;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    /// <inheritdoc/>
    public async Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤 1：校验时间区间，避免无效区间进入仓储查询。
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new GlobalDashboardQueryResult();
        }

        // 步骤 2：按时间区间执行仓储侧聚合，避免全量任务加载后内存聚合。
        var cacheKey = $"global-dashboard:{request.StartTimeLocal:yyyyMMddHHmmssfffffff}:{request.EndTimeLocal:yyyyMMddHHmmssfffffff}";
        if (_queryCacheOptions.Enabled)
        {
            var ttl = Math.Clamp(_queryCacheOptions.GlobalDashboardSeconds, 1, 60);
            var cachedResult = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl);
                return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
            });
            return cachedResult ?? new GlobalDashboardQueryResult();
        }

        return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
    }

    /// <summary>
    /// 构建总看板查询结果。
    /// </summary>
    /// <param name="startTimeLocal">开始时间。</param>
    /// <param name="endTimeLocal">结束时间。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>总看板结果。</returns>
    private async Task<GlobalDashboardQueryResult> BuildResultAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken cancellationToken)
    {
        var waveRows = await _businessTaskRepository.AggregateWaveDashboardAsync(startTimeLocal, endTimeLocal, cancellationToken);
        var totalCount = waveRows.Sum(row => row.TotalCount);
        var unsortedCount = waveRows.Sum(row => row.UnsortedCount);
        var fullCaseTotalCount = waveRows.Sum(row => row.FullCaseTotalCount);
        var fullCaseUnsortedCount = waveRows.Sum(row => row.FullCaseUnsortedCount);
        var splitTotalCount = waveRows.Sum(row => row.SplitTotalCount);
        var splitUnsortedCount = waveRows.Sum(row => row.SplitUnsortedCount);
        var recognitionCount = waveRows.Sum(row => row.RecognitionCount);
        var recirculatedCount = waveRows.Sum(row => row.RecirculatedCount);
        var exceptionCount = waveRows.Sum(row => row.ExceptionCount);
        var totalVolumeMm3 = waveRows.Sum(row => row.TotalVolumeMm3);
        var totalWeightGram = waveRows.Sum(row => row.TotalWeightGram);
        var waveSummaries = BuildWaveSummaries(waveRows);

        // 步骤 3：输出标准化查询结果。
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
            RecognitionRatePercent = CalculateRatePercent(recognitionCount, totalCount),
            RecirculatedCount = recirculatedCount,
            ExceptionCount = exceptionCount,
            TotalVolumeMm3 = totalVolumeMm3,
            TotalWeightGram = totalWeightGram,
            WaveSummaries = waveSummaries
        };
    }

    /// <summary>
    /// 根据波次聚合行构建波次维度统计结果。
    /// </summary>
    /// <param name="waveRows">波次聚合行。</param>
    /// <returns>波次统计集合。</returns>
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

    /// <summary>
    /// 计算分拣进度百分比。
    /// </summary>
    /// <param name="totalCount">总数。</param>
    /// <param name="unsortedCount">未分拣数量。</param>
    /// <returns>百分比。</returns>
    private static decimal CalculateProgressPercent(int totalCount, int unsortedCount)
    {
        if (totalCount <= 0)
        {
            return 0M;
        }

        var sortedCount = totalCount - unsortedCount;
        return Math.Round((decimal)sortedCount * 100M / totalCount, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 计算比率百分比。
    /// </summary>
    /// <param name="numerator">分子。</param>
    /// <param name="denominator">分母。</param>
    /// <returns>百分比。</returns>
    private static decimal CalculateRatePercent(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0M;
        }

        return Math.Round((decimal)numerator * 100M / denominator, 2, MidpointRounding.AwayFromZero);
    }

}
