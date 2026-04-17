using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

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

        // 步骤 2：在仓储侧下推波次选项查询。
        var waveOptions = await _businessTaskRepository.ListWaveCodesByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);

        // 步骤 3：在仓储侧按可选波次聚合码头指标。
        var selectedWaveCode = string.IsNullOrWhiteSpace(request.WaveCode) ? null : request.WaveCode.Trim();
        var dockRows = await _businessTaskRepository.AggregateDockDashboardAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
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
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            SelectedWaveCode = selectedWaveCode,
            WaveOptions = waveOptions,
            DockSummaries = summaries
        };
    }
}
