using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 StubWaveQueryService 类型。
/// </summary>
internal sealed class StubWaveQueryService : IWaveQueryService
{
    /// <summary>
    /// 获取或设置 LastCurrentRequest。
    /// </summary>
    public CurrentWaveQueryRequest? LastCurrentRequest { get; private set; }

    /// <summary>
    /// 获取或设置 LastOptionsRequest。
    /// </summary>
    public WaveOptionsQueryRequest? LastOptionsRequest { get; private set; }

    /// <summary>
    /// 获取或设置 LastSummaryRequest。
    /// </summary>
    public WaveSummaryQueryRequest? LastSummaryRequest { get; private set; }

    /// <summary>
    /// 获取或设置 LastZoneRequest。
    /// </summary>
    public WaveZoneQueryRequest? LastZoneRequest { get; private set; }

    /// <summary>
    /// 获取或设置 LastListRequest。
    /// </summary>
    public WaveListQueryRequest? LastListRequest { get; private set; }

    /// <summary>
    /// 获取或设置 LastDetailRequest。
    /// </summary>
    public WaveDetailQueryRequest? LastDetailRequest { get; private set; }

    /// <summary>
    /// 获取或设置 LastCleanupWaveCode。
    /// </summary>
    public string? LastCleanupWaveCode { get; private set; }

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
                ZoneName = "拆零区1",
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
                UnsortedCount = 2,
                SplitTotal = 6,
                FullCaseTotal = 4,
                SplitRatioPercent = 60M,
                FullCaseRatioPercent = 40M,
                RecirculatedCount = 3,
                ExceptionCount = 1,
                CreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 0, 0), DateTimeKind.Local),
                Status = "分拣中"
            }
        ]
    };

    public WaveCleanupQueryResult CleanupResult { get; set; } = new()
    {
        Items =
        [
            new WaveCleanupWaveItem
            {
                WaveCode = "W1",
                WaveRemark = "Remark1",
                PackageTotal = 10,
                SplitTotal = 6,
                FullCaseTotal = 4,
                CreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 0, 0), DateTimeKind.Local),
                Status = "分拣中"
            }
        ]
    };

    public WaveDetailQueryResult DetailResult { get; set; } = new()
    {
        StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
        EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
        WaveCode = "W1",
        WaveRemark = "Remark1",
        Items =
        [
            new WaveDetailItem
            {
                TaskCode = "TASK-001",
                WaveCode = "W1",
                WaveRemark = "Remark1",
                SourceType = "整件",
                WorkingArea = "1",
                Barcode = "BC-001",
                OrderId = "ORDER-001",
                StoreId = "STORE-001",
                StoreName = "Store 1",
                ProductCode = "SKU-001",
                PickLocation = "A-01-01",
                ChuteCode = "8",
                Status = "已扫描",
                IsRecirculated = true,
                IsException = false,
                ScannedAtLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 30, 0), DateTimeKind.Local),
                CreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 0, 0), DateTimeKind.Local),
                UpdatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 35, 0), DateTimeKind.Local)
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
        return Task.FromResult("区域名称,总数,待分拣数,进度百分比,回流数,异常数\r\n拆零区1,1,0,100,0,0\r\n");
    }

    public Task<string> ExportListCsvAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
    {
        LastListRequest = request;
        return Task.FromResult("波次号,备注,包裹总数,待分拣数,拆零总数,整件总数,拆零占比百分比,整件占比百分比,回流数,异常数,创建时间,状态\r\nW1,Remark1,10,2,6,4,60,40,3,1,2026-04-20 08:00:00,分拣中\r\n");
    }

    public Task<WaveCleanupQueryResult> QueryCleanupWaveAsync(string? waveCode, CancellationToken cancellationToken)
    {
        LastCleanupWaveCode = waveCode;
        return Task.FromResult(CleanupResult);
    }

    public Task<WaveDetailQueryResult> QueryDetailsAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken)
    {
        LastDetailRequest = request;
        return Task.FromResult(DetailResult);
    }

    public Task<string> ExportDetailsCsvAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken)
    {
        LastDetailRequest = request;
        return Task.FromResult("任务编码,波次号,波次备注,来源类型,作业区域,条码,订单号,门店号,门店名称,商品编码,拣货位,格口,状态,是否回流,是否异常,扫描时间,创建时间,更新时间\r\nTASK-001,W1,Remark1,整件,1,BC-001,ORDER-001,STORE-001,Store 1,SKU-001,A-01-01,8,已扫描,是,否,2026-04-20 08:30:00,2026-04-20 08:00:00,2026-04-20 08:35:00\r\n");
    }
}

