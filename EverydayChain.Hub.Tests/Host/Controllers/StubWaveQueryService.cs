using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 波次查询服务替身。
/// </summary>
internal sealed class StubWaveQueryService : IWaveQueryService
{
    /// <summary>
    /// 最近一次波次选项请求。
    /// </summary>
    public WaveOptionsQueryRequest? LastOptionsRequest { get; private set; }

    /// <summary>
    /// 最近一次波次摘要请求。
    /// </summary>
    public WaveSummaryQueryRequest? LastSummaryRequest { get; private set; }

    /// <summary>
    /// 最近一次波次分区请求。
    /// </summary>
    public WaveZoneQueryRequest? LastZoneRequest { get; private set; }

    /// <summary>
    /// 固定波次选项结果。
    /// </summary>
    public WaveOptionsQueryResult OptionsResult { get; set; } = new()
    {
        StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
        EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
        WaveOptions =
        [
            new WaveOptionItem
            {
                WaveCode = "W1",
                WaveRemark = "备注1"
            }
        ]
    };

    /// <summary>
    /// 固定波次摘要结果。
    /// </summary>
    public WaveSummaryQueryResult? SummaryResult { get; set; } = new()
    {
        WaveCode = "W1",
        WaveRemark = "备注1",
        TotalCount = 10,
        UnsortedCount = 2,
        SortedProgressPercent = 80M,
        RecirculatedCount = 3,
        ExceptionCount = 1
    };

    /// <summary>
    /// 固定波次分区结果。
    /// </summary>
    public WaveZoneQueryResult? ZoneResult { get; set; } = new()
    {
        WaveCode = "W1",
        WaveRemark = "备注1",
        Zones =
        [
            new WaveZoneSummary
            {
                ZoneCode = "SplitZone1",
                ZoneName = "拆零1区",
                TotalCount = 1,
                UnsortedCount = 0,
                SortedProgressPercent = 100M,
                RecirculatedCount = 0,
                ExceptionCount = 0
            }
        ]
    };

    /// <inheritdoc/>
    public Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken)
    {
        LastOptionsRequest = request;
        return Task.FromResult(OptionsResult);
    }

    /// <inheritdoc/>
    public Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        LastSummaryRequest = request;
        return Task.FromResult(SummaryResult);
    }

    /// <inheritdoc/>
    public Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
    {
        LastZoneRequest = request;
        return Task.FromResult(ZoneResult);
    }
}
