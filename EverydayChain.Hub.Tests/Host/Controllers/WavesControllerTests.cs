using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 WavesControllerTests 类型。
/// </summary>
public sealed class WavesControllerTests
{
    [Fact]
    public async Task QueryCurrentAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        var controller = new WavesController(stubService);
        var request = new CurrentWaveQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.QueryCurrentAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<CurrentWaveResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("W1", response.Data!.WaveCode);
        Assert.NotNull(stubService.LastCurrentRequest);
    }

    [Fact]
    public async Task QueryOptionsAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        var controller = new WavesController(stubService);
        var request = new WaveOptionsQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.QueryOptionsAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<WaveOptionsResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastOptionsRequest);
        Assert.Equal(request.StartTimeLocal, stubService.LastOptionsRequest!.StartTimeLocal);
        Assert.Single(response.Data!.WaveOptions);
    }

    [Fact]
    public async Task QuerySummaryAsync_ShouldReturnBadRequest_WhenWaveCodeIsEmpty()
    {
        var controller = new WavesController(new StubWaveQueryService());
        var request = new WaveSummaryQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            WaveCode = " "
        };

        var result = await controller.QuerySummaryAsync(request, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task QueryZonesAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        stubService.ZoneResult = new EverydayChain.Hub.Application.Models.WaveZoneQueryResult
        {
            WaveCode = "W1",
            WaveRemark = "Remark1",
            Zones =
            [
                new EverydayChain.Hub.Application.Models.WaveZoneSummary
                {
                    ZoneCode = "SplitZone1",
                    ZoneName = "拆零区1",
                    TotalCount = 0,
                    UnsortedCount = 0,
                    SortedProgressPercent = 0M,
                    RecirculatedCount = 0,
                    ExceptionCount = 0
                },
                new EverydayChain.Hub.Application.Models.WaveZoneSummary
                {
                    ZoneCode = "FullCase",
                    ZoneName = "整件区",
                    TotalCount = 0,
                    UnsortedCount = 0,
                    SortedProgressPercent = 0M,
                    RecirculatedCount = 0,
                    ExceptionCount = 0
                }
            ]
        };
        var controller = new WavesController(stubService);
        var request = new WaveZoneQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "W1"
        };

        var result = await controller.QueryZonesAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<WaveZoneResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastZoneRequest);
        Assert.Equal("W1", stubService.LastZoneRequest!.WaveCode);
        Assert.Equal(2, response.Data!.Zones.Count);
    }

    [Fact]
    public async Task QueryListAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        var controller = new WavesController(stubService);
        var request = new WaveListQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.QueryListAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<WaveListResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Single(response.Data!.Items);
        Assert.Equal("W1", response.Data.Items[0].WaveId);
        Assert.Equal(10, response.Data.Items[0].PackageTotal);
        Assert.Equal(2, response.Data.Items[0].UnsortedCount);
        Assert.Equal(60M, response.Data.Items[0].SplitRatioPercent);
        Assert.Equal(3, response.Data.Items[0].RecirculatedCount);
        Assert.Equal("分拣中", response.Data.Items[0].Status);
        Assert.NotNull(stubService.LastListRequest);
    }

    [Fact]
    public async Task QueryDetailsAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        var controller = new WavesController(stubService);
        var request = new WaveDetailQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "W1"
        };

        var result = await controller.QueryDetailsAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<WaveDetailResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("W1", response.Data!.WaveCode);
        Assert.Single(response.Data.Items);
        Assert.Equal("ORDER-001", response.Data.Items[0].OrderId);
        Assert.Equal("SKU-001", response.Data.Items[0].ProductCode);
        Assert.NotNull(stubService.LastDetailRequest);
    }

    [Fact]
    public async Task ExportZonesCsvAsync_ShouldReturnFile_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        var controller = new WavesController(stubService);
        var request = new WaveZoneQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "W1"
        };

        var result = await controller.ExportZonesCsvAsync(request, null, CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.NotNull(stubService.LastZoneRequest);
        Assert.Equal("W1", stubService.LastZoneRequest!.WaveCode);
    }

    [Fact]
    public async Task ExportDetailsCsvAsync_ShouldReturnFile_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        var controller = new WavesController(stubService);
        var request = new WaveDetailQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "W1"
        };

        var result = await controller.ExportDetailsCsvAsync(request, null, CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.NotNull(stubService.LastDetailRequest);
        Assert.Equal("W1", stubService.LastDetailRequest!.WaveCode);
    }

    [Fact]
    public async Task ExportListXlsxAsync_ShouldReturnFile_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        var controller = new WavesController(stubService);
        var request = new WaveListQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.ExportListXlsxAsync(request, null, CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.NotNull(stubService.LastListRequest);
    }
}

