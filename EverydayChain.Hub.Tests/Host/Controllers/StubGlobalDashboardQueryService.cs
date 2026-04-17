using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 总看板查询服务替身。
/// </summary>
internal sealed class StubGlobalDashboardQueryService : IGlobalDashboardQueryService
{
    /// <summary>
    /// 最近一次查询请求。
    /// </summary>
    public GlobalDashboardQueryRequest? LastRequest { get; private set; }

    /// <summary>
    /// 固定返回结果。
    /// </summary>
    public GlobalDashboardQueryResult Result { get; set; } = new()
    {
        TotalCount = 10,
        UnsortedCount = 3,
        TotalSortedProgressPercent = 70M,
        FullCaseTotalCount = 4,
        FullCaseUnsortedCount = 1,
        FullCaseSortedProgressPercent = 75M,
        SplitTotalCount = 6,
        SplitUnsortedCount = 2,
        SplitSortedProgressPercent = 66.67M,
        RecognitionRatePercent = 90M,
        RecirculatedCount = 2,
        ExceptionCount = 1,
        TotalVolumeMm3 = 5000M,
        TotalWeightGram = 1200M,
        WaveSummaries = [new WaveDashboardSummary
        {
            WaveCode = "WAVE-001",
            TotalCount = 10,
            UnsortedCount = 3,
            SortedProgressPercent = 70M
        }]
    };

    /// <inheritdoc/>
    public Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }
}
