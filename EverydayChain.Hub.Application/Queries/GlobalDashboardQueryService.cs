using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 总看板查询服务实现。
/// </summary>
public sealed class GlobalDashboardQueryService : IGlobalDashboardQueryService
{
    /// <summary>
    /// 无波次标识占位值。
    /// </summary>
    private const string EmptyWaveCode = "未分波次";

    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 初始化总看板查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    public GlobalDashboardQueryService(IBusinessTaskRepository businessTaskRepository)
    {
        _businessTaskRepository = businessTaskRepository;
    }

    /// <inheritdoc/>
    public async Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤 1：校验时间区间，避免无效区间进入仓储查询。
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new GlobalDashboardQueryResult();
        }

        // 步骤 2：按时间区间查询业务任务。
        var tasks = await _businessTaskRepository.FindByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);

        // 步骤 3：单次遍历聚合总量、分口径、识别率、回流、异常与测量指标，并同步构建波次聚合。
        var totalCount = 0;
        var unsortedCount = 0;
        var fullCaseTotalCount = 0;
        var fullCaseUnsortedCount = 0;
        var splitTotalCount = 0;
        var splitUnsortedCount = 0;
        var recognitionCount = 0;
        var recirculatedCount = 0;
        var exceptionCount = 0;
        decimal totalVolumeMm3 = 0M;
        decimal totalWeightGram = 0M;
        var waveCounters = new Dictionary<string, WaveCounter>(StringComparer.OrdinalIgnoreCase);

        foreach (var task in tasks)
        {
            totalCount++;
            var isSortedTask = IsSortedTask(task);
            if (!isSortedTask)
            {
                unsortedCount++;
            }

            switch (task.SourceType)
            {
                case BusinessTaskSourceType.FullCase:
                    fullCaseTotalCount++;
                    if (!isSortedTask)
                    {
                        fullCaseUnsortedCount++;
                    }
                    break;
                case BusinessTaskSourceType.Split:
                    splitTotalCount++;
                    if (!isSortedTask)
                    {
                        splitUnsortedCount++;
                    }
                    break;
            }

            if (task.ScannedAtLocal.HasValue)
            {
                recognitionCount++;
            }

            if (task.IsRecirculated)
            {
                recirculatedCount++;
            }

            if (task.IsException || task.Status == BusinessTaskStatus.Exception)
            {
                exceptionCount++;
            }

            totalVolumeMm3 += task.VolumeMm3 ?? 0M;
            totalWeightGram += task.WeightGram ?? 0M;

            var waveCode = string.IsNullOrWhiteSpace(task.WaveCode) ? EmptyWaveCode : task.WaveCode.Trim();
            if (!waveCounters.TryGetValue(waveCode, out var waveCounter))
            {
                waveCounter = new WaveCounter();
                waveCounters.Add(waveCode, waveCounter);
            }

            waveCounter.TotalCount++;
            if (!isSortedTask)
            {
                waveCounter.UnsortedCount++;
            }
        }

        var waveSummaries = BuildWaveSummaries(waveCounters);

        // 步骤 4：输出标准化查询结果。
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
    /// 根据聚合计数构建波次维度统计结果。
    /// </summary>
    /// <param name="waveCounters">波次聚合计数字典。</param>
    /// <returns>波次统计集合。</returns>
    private static IReadOnlyList<WaveDashboardSummary> BuildWaveSummaries(Dictionary<string, WaveCounter> waveCounters)
    {
        return waveCounters
            .Select(pair =>
            {
                var totalCount = pair.Value.TotalCount;
                var unsortedCount = pair.Value.UnsortedCount;
                return new WaveDashboardSummary
                {
                    WaveCode = pair.Key,
                    TotalCount = totalCount,
                    UnsortedCount = unsortedCount,
                    SortedProgressPercent = CalculateProgressPercent(totalCount, unsortedCount)
                };
            })
            .OrderBy(summary => summary.WaveCode, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 判断任务是否已分拣完成。
    /// </summary>
    /// <param name="task">业务任务。</param>
    /// <returns>是否已分拣。</returns>
    private static bool IsSortedTask(BusinessTaskEntity task)
    {
        return task.Status == BusinessTaskStatus.Dropped
            || task.Status == BusinessTaskStatus.FeedbackPending;
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

    /// <summary>
    /// 波次聚合计数器。
    /// </summary>
    private sealed class WaveCounter
    {
        /// <summary>
        /// 波次总量。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 波次未分拣数量。
        /// </summary>
        public int UnsortedCount { get; set; }
    }
}
