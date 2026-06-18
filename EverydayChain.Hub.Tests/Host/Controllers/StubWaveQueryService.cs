using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

internal sealed class StubWaveQueryService : IWaveQueryService
{
    public CurrentWaveQueryRequest? LastCurrentRequest { get; private set; }

    public WaveOptionsQueryRequest? LastOptionsRequest { get; private set; }

    public WaveSummaryQueryRequest? LastSummaryRequest { get; private set; }

    public WaveZoneQueryRequest? LastZoneRequest { get; private set; }

    public WaveListQueryRequest? LastListRequest { get; private set; }

    public CurrentWaveQueryResult CurrentResult { get; set; } = new()
    {
        StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
        EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
        WaveCode = "W1",
        WaveRemark = "Remark1",
        Barcode = "BC-001",
        ScanTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 0, 0), DateTimeKind.Local)
    };

    public WaveOptionsQueryResult OptionsResult { get; set; } = new()
    {
        StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
        EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
        WaveOptions =
        [
            new WaveOptionItem
            {
                WaveCode = "W1",
                WaveRemark = "Remark1"
            }
        ]
    };

    public WaveSummaryQueryResult? SummaryResult { get; set; } = new()
    {
        WaveCode = "W1",
        WaveRemark = "Remark1",
        TotalCount = 10,
        UnsortedCount = 2,
        SortedProgressPercent = 80M,
        RecirculatedCount = 3,
        ExceptionCount = 1
    };

    public WaveZoneQueryResult? ZoneResult { get; set; } = new()
    {
        WaveCode = "W1",
        WaveRemark = "Remark1",
        Zones =
        [
            new WaveZoneSummary
            {
                ZoneCode = "SplitZone1",
                ZoneName = "Split Zone 1",
                TotalCount = 1,
                UnsortedCount = 0,
                SortedProgressPercent = 100M,
                RecirculatedCount = 0,
                ExceptionCount = 0
            }
        ]
    };

    public WaveListQueryResult ListResult { get; set; } = new()
    {
        StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
        EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
        Items =
        [
            new WaveListItem
            {
                WaveCode = "W1",
                WaveRemark = "Remark1",
                PackageTotal = 10,
                SplitTotal = 6,
                FullCaseTotal = 4,
                CreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 0, 0), DateTimeKind.Local),
                Status = "Sorting"
            }
        ]
    };

    public Task<CurrentWaveQueryResult> QueryCurrentAsync(CurrentWaveQueryRequest request, CancellationToken cancellationToken)
    {
        LastCurrentRequest = request;
        return Task.FromResult(CurrentResult);
    }

    public Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken)
    {
        LastOptionsRequest = request;
        return Task.FromResult(OptionsResult);
    }

    public Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        LastSummaryRequest = request;
        return Task.FromResult(SummaryResult);
    }

    public Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
    {
        LastZoneRequest = request;
        return Task.FromResult(ZoneResult);
    }

    public Task<WaveListQueryResult> QueryListAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
    {
        LastListRequest = request;
        return Task.FromResult(ListResult);
    }

    public Task<string> ExportZonesCsvAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
    {
        LastZoneRequest = request;
        return Task.FromResult("ZoneName,TotalCount,PendingCount,ProgressPercent,RecirculatedCount,ExceptionCount\r\nSplit Zone 1,1,0,100,0,0\r\n");
    }

    public Task<string> ExportListCsvAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
    {
        LastListRequest = request;
        return Task.FromResult("WaveId,Remark,PackageTotal,SplitTotal,FullTotal,CreatedAt,Status\r\nW1,Remark1,10,6,4,2026-04-20 08:00:00,Sorting\r\n");
    }
}
