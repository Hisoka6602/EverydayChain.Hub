using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
internal sealed class StubDockDashboardQueryService : IDockDashboardQueryService
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DockDashboardQueryRequest? LastRequest { get; private set; }

    public DockDashboardQueryResult Result { get; set; } = new()
    {
        StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
        EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
        WaveOptions = ["W1"],
        SelectedWaveCode = "W1",
        DockSummaries = [new DockDashboardSummary
        {
            DockCode = "7",
            SplitUnsortedCount = 1,
            FullCaseUnsortedCount = 2,
            RecirculatedCount = 3,
            ExceptionCount = 1,
            SortedProgressPercent = 50M,
            SortedCount = 4
        }]
    };

    public Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }
}

