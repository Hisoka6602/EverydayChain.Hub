using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 码头看板查询服务实现。
/// </summary>
public sealed class DockDashboardQueryService : IDockDashboardQueryService
{
    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 业务任务统计规则。
    /// </summary>
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();

    /// <summary>
    /// 内存缓存。
    /// </summary>
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// 查询缓存配置。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;

    /// <summary>
    /// 初始化码头看板查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    public DockDashboardQueryService(IBusinessTaskRepository businessTaskRepository)
        : this(businessTaskRepository, new MemoryCache(new MemoryCacheOptions()), new QueryCacheOptions())
    {
    }

    /// <summary>
    /// 初始化码头看板查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="memoryCache">内存缓存。</param>
    /// <param name="queryCacheOptions">缓存配置。</param>
    public DockDashboardQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        _businessTaskRepository = businessTaskRepository;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    /// <inheritdoc/>
    public async Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤 1：校验时间区间，防止无效查询进入仓储层。
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new DockDashboardQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        var selectedWaveCode = string.IsNullOrWhiteSpace(request.WaveCode) ? null : request.WaveCode.Trim();
        var cacheKey = $"dock-dashboard:{request.StartTimeLocal:yyyyMMddHHmmssfffffff}:{request.EndTimeLocal:yyyyMMddHHmmssfffffff}:{selectedWaveCode}";
        if (_queryCacheOptions.Enabled)
        {
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

        return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedWaveCode, cancellationToken);
    }

    /// <summary>
    /// 构建码头看板查询结果。
    /// </summary>
    /// <param name="startTimeLocal">开始时间。</param>
    /// <param name="endTimeLocal">结束时间。</param>
    /// <param name="selectedWaveCode">选中波次。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>码头看板结果。</returns>
    private async Task<DockDashboardQueryResult> BuildResultAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? selectedWaveCode,
        CancellationToken cancellationToken)
    {
        // 步骤 1：在仓储侧下推波次选项查询。
        var waveOptions = await _businessTaskRepository.ListWaveCodesByCreatedTimeRangeAsync(startTimeLocal, endTimeLocal, cancellationToken);

        // 步骤 2：在仓储侧按可选波次聚合码头指标。
        var dockRows = await _businessTaskRepository.AggregateDockDashboardAsync(
            startTimeLocal,
            endTimeLocal,
            selectedWaveCode,
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

        // 步骤 4：返回标准化看板结果。
        return new DockDashboardQueryResult
        {
            StartTimeLocal = startTimeLocal,
            EndTimeLocal = endTimeLocal,
            SelectedWaveCode = selectedWaveCode,
            WaveOptions = waveOptions,
            DockSummaries = summaries
        };
    }
}
