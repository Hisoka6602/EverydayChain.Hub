using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 波次查询控制器测试。
/// </summary>
public sealed class WavesControllerTests
{
    /// <summary>
    /// 波次选项接口应返回成功结果。
    /// </summary>
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

    /// <summary>
    /// 波次摘要接口应在缺失波次号时返回失败。
    /// </summary>
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

    /// <summary>
    /// 波次分区接口应返回固定分区结果。
    /// </summary>
    [Fact]
    public async Task QueryZonesAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubWaveQueryService();
        stubService.ZoneResult = new EverydayChain.Hub.Application.Models.WaveZoneQueryResult
        {
            WaveCode = "W1",
            WaveRemark = "备注1",
            Zones =
            [
                new EverydayChain.Hub.Application.Models.WaveZoneSummary
                {
                    ZoneCode = "SplitZone1",
                    ZoneName = "拆零1区",
                    TotalCount = 0,
                    UnsortedCount = 0,
                    SortedProgressPercent = 0M,
                    RecirculatedCount = 0,
                    ExceptionCount = 0
                },
                new EverydayChain.Hub.Application.Models.WaveZoneSummary
                {
                    ZoneCode = "SplitZone2",
                    ZoneName = "拆零2区",
                    TotalCount = 0,
                    UnsortedCount = 0,
                    SortedProgressPercent = 0M,
                    RecirculatedCount = 0,
                    ExceptionCount = 0
                },
                new EverydayChain.Hub.Application.Models.WaveZoneSummary
                {
                    ZoneCode = "SplitZone3",
                    ZoneName = "拆零3区",
                    TotalCount = 0,
                    UnsortedCount = 0,
                    SortedProgressPercent = 0M,
                    RecirculatedCount = 0,
                    ExceptionCount = 0
                },
                new EverydayChain.Hub.Application.Models.WaveZoneSummary
                {
                    ZoneCode = "SplitZone4",
                    ZoneName = "拆零4区",
                    TotalCount = 0,
                    UnsortedCount = 0,
                    SortedProgressPercent = 0M,
                    RecirculatedCount = 0,
                    ExceptionCount = 0
                },
                new EverydayChain.Hub.Application.Models.WaveZoneSummary
                {
                    ZoneCode = "FullCase",
                    ZoneName = "整件数据",
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
        Assert.Equal(5, response.Data!.Zones.Count);
    }
}
