using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;

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
    /// 初始化码头看板查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    public DockDashboardQueryService(IBusinessTaskRepository businessTaskRepository)
    {
        _businessTaskRepository = businessTaskRepository;
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

        // 步骤 2：拉取时间区间内任务并构建波次选项。
        var tasks = await _businessTaskRepository.FindByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
        var waveOptions = tasks
            .Select(task => _queryPolicy.NormalizeWaveCode(task.WaveCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        // 步骤 3：按可选波次过滤，再按码头聚合指标。
        var selectedWaveCode = string.IsNullOrWhiteSpace(request.WaveCode) ? null : request.WaveCode.Trim();
        var filteredTasks = string.IsNullOrWhiteSpace(selectedWaveCode)
            ? tasks
            : tasks.Where(task => string.Equals(_queryPolicy.NormalizeWaveCode(task.WaveCode), selectedWaveCode, StringComparison.OrdinalIgnoreCase)).ToList();

        var counters = new Dictionary<string, DockCounter>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in filteredTasks)
        {
            var dockCode = _queryPolicy.ResolveDockCode(task);
            if (!counters.TryGetValue(dockCode, out var counter))
            {
                counter = new DockCounter();
                counters.Add(dockCode, counter);
            }

            counter.TotalCount++;
            var isSorted = _queryPolicy.IsSortedTask(task);
            if (isSorted)
            {
                counter.SortedCount++;
            }
            else if (task.SourceType == BusinessTaskSourceType.Split)
            {
                counter.SplitUnsortedCount++;
            }
            else if (task.SourceType == BusinessTaskSourceType.FullCase)
            {
                counter.FullCaseUnsortedCount++;
            }

            if (task.IsRecirculated)
            {
                counter.RecirculatedCount++;
            }

            if (_queryPolicy.IsDockSeven(dockCode) && (task.IsException || task.Status == BusinessTaskStatus.Exception))
            {
                counter.ExceptionCount++;
            }
        }

        var summaries = counters
            .Select(pair => new DockDashboardSummary
            {
                DockCode = pair.Key,
                SplitUnsortedCount = pair.Value.SplitUnsortedCount,
                FullCaseUnsortedCount = pair.Value.FullCaseUnsortedCount,
                RecirculatedCount = pair.Value.RecirculatedCount,
                ExceptionCount = pair.Value.ExceptionCount,
                SortedCount = pair.Value.SortedCount,
                SortedProgressPercent = _queryPolicy.CalculatePercent(pair.Value.SortedCount, pair.Value.TotalCount)
            })
            .OrderBy(summary => summary.DockCode, StringComparer.Ordinal)
            .ToList();

        // 步骤 4：返回标准化看板结果。
        return new DockDashboardQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            SelectedWaveCode = selectedWaveCode,
            WaveOptions = waveOptions,
            DockSummaries = summaries
        };
    }

    /// <summary>
    /// 码头聚合计数器。
    /// </summary>
    private sealed class DockCounter
    {
        /// <summary>
        /// 码头总任务数。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 拆零未分拣数。
        /// </summary>
        public int SplitUnsortedCount { get; set; }

        /// <summary>
        /// 整件未分拣数。
        /// </summary>
        public int FullCaseUnsortedCount { get; set; }

        /// <summary>
        /// 回流数。
        /// </summary>
        public int RecirculatedCount { get; set; }

        /// <summary>
        /// 异常数。
        /// </summary>
        public int ExceptionCount { get; set; }

        /// <summary>
        /// 已分拣数。
        /// </summary>
        public int SortedCount { get; set; }
    }
}
