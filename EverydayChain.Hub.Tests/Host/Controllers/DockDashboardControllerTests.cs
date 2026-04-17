using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 码头看板控制器测试。
/// </summary>
public sealed class DockDashboardControllerTests
{
    /// <summary>
    /// 有效请求应返回看板结果。
    /// </summary>
    [Fact]
    public async Task QueryOverviewAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubDockDashboardQueryService();
        var controller = new DockDashboardController(stubService);
        var request = new DockDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "W1"
        };

        var result = await controller.QueryOverviewAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DockDashboardResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal("W1", stubService.LastRequest!.WaveCode);
    }

    /// <summary>
    /// 结束时间不大于开始时间应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task QueryOverviewAsync_ShouldReturnBadRequest_WhenTimeRangeInvalid()
    {
        var controller = new DockDashboardController(new StubDockDashboardQueryService());
        var request = new DockDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.QueryOverviewAsync(request, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// 仅传开始时间时应自动补齐结束时间为开始时间加一天。
    /// </summary>
    [Fact]
    public async Task QueryOverviewAsync_ShouldDefaultEndTimeToStartPlusOneDay_WhenOnlyStartTimeProvided()
    {
        var stubService = new StubDockDashboardQueryService();
        var controller = new DockDashboardController(stubService);
        var startTime = DateTime.SpecifyKind(new DateTime(2026, 4, 19, 8, 0, 0), DateTimeKind.Local);
        var request = new DockDashboardQueryRequest
        {
            StartTimeLocal = startTime
        };

        var result = await controller.QueryOverviewAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _ = Assert.IsType<ApiResponse<DockDashboardResponse>>(okResult.Value);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(startTime, stubService.LastRequest!.StartTimeLocal);
        Assert.Equal(startTime.AddDays(1), stubService.LastRequest.EndTimeLocal);
    }
}
