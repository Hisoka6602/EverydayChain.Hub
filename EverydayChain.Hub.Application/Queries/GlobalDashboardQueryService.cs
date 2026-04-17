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

        // 步骤 3：按总量、整件、拆零维度计算核心指标。
        var totalCount = tasks.Count;
        var unsortedCount = tasks.Count(task => !IsSortedTask(task));
        var fullCaseTasks = tasks.Where(task => task.SourceType == BusinessTaskSourceType.FullCase).ToList();
        var splitTasks = tasks.Where(task => task.SourceType == BusinessTaskSourceType.Split).ToList();

        // 步骤 4：计算识别率、回流、异常与测量累计数据。
        var recognitionCount = tasks.Count(task => task.ScannedAtLocal.HasValue);
        var exceptionCount = tasks.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception);
        var waveSummaries = BuildWaveSummaries(tasks);

        // 步骤 5：输出标准化查询结果。
        return new GlobalDashboardQueryResult
        {
            TotalCount = totalCount,
            UnsortedCount = unsortedCount,
            TotalSortedProgressPercent = CalculateProgressPercent(totalCount, unsortedCount),
            FullCaseTotalCount = fullCaseTasks.Count,
            FullCaseUnsortedCount = fullCaseTasks.Count(task => !IsSortedTask(task)),
            FullCaseSortedProgressPercent = CalculateProgressPercent(fullCaseTasks.Count, fullCaseTasks.Count(task => !IsSortedTask(task))),
            SplitTotalCount = splitTasks.Count,
            SplitUnsortedCount = splitTasks.Count(task => !IsSortedTask(task)),
            SplitSortedProgressPercent = CalculateProgressPercent(splitTasks.Count, splitTasks.Count(task => !IsSortedTask(task))),
            RecognitionRatePercent = CalculateRatePercent(recognitionCount, totalCount),
            RecirculatedCount = tasks.Count(task => task.IsRecirculated),
            ExceptionCount = exceptionCount,
            TotalVolumeMm3 = tasks.Sum(task => task.VolumeMm3 ?? 0M),
            TotalWeightGram = tasks.Sum(task => task.WeightGram ?? 0M),
            WaveSummaries = waveSummaries
        };
    }

    /// <summary>
    /// 计算波次维度聚合结果。
    /// </summary>
    /// <param name="tasks">任务集合。</param>
    /// <returns>波次统计集合。</returns>
    private static IReadOnlyList<WaveDashboardSummary> BuildWaveSummaries(IReadOnlyList<BusinessTaskEntity> tasks)
    {
        return tasks
            .GroupBy(task => string.IsNullOrWhiteSpace(task.WaveCode) ? EmptyWaveCode : task.WaveCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var totalCount = group.Count();
                var unsortedCount = group.Count(task => !IsSortedTask(task));
                return new WaveDashboardSummary
                {
                    WaveCode = group.Key,
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
}
