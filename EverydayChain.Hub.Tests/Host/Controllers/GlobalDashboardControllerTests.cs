using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 GlobalDashboardControllerTests 类型。
/// </summary>
public sealed class GlobalDashboardControllerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task QueryOverviewAsync_ShouldReturnBadRequest_WhenTimeIsMinValue(bool useMinStartTime)
    {
        var controller = new GlobalDashboardController(new StubGlobalDashboardQueryService());
        var request = new GlobalDashboardQueryRequest
        {
            StartTimeLocal = useMinStartTime
                ? DateTime.MinValue
                : DateTime.SpecifyKind(new DateTime(2026, 4, 17, 9, 0, 0), DateTimeKind.Local),
            EndTimeLocal = useMinStartTime
                ? DateTime.SpecifyKind(new DateTime(2026, 4, 17, 10, 0, 0), DateTimeKind.Local)
                : DateTime.MinValue
        };

        var actionResult = await controller.QueryOverviewAsync(request, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task QueryOverviewAsync_ShouldReturnBadRequest_WhenEndTimeIsNotGreaterThanStartTime()
    {
        var controller = new GlobalDashboardController(new StubGlobalDashboardQueryService());
        var request = new GlobalDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 10, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 10, 0, 0), DateTimeKind.Local)
        };

        var actionResult = await controller.QueryOverviewAsync(request, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task QueryOverviewAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubGlobalDashboardQueryService();
        var controller = new GlobalDashboardController(stubService);
        var request = new GlobalDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 9, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 10, 0, 0), DateTimeKind.Local)
        };

        var actionResult = await controller.QueryOverviewAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<GlobalDashboardResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(10, response.Data.TotalCount);
        Assert.Single(response.Data.WaveSummaries);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(request.StartTimeLocal, stubService.LastRequest!.StartTimeLocal);
        Assert.Equal(request.EndTimeLocal, stubService.LastRequest.EndTimeLocal);
    }

    [Fact]
    public async Task QueryOverviewAsync_ShouldUseQueryRequest_WhenBodyRequestIsNull()
    {
        var stubService = new StubGlobalDashboardQueryService();
        var controller = new GlobalDashboardController(stubService);
        var queryRequest = new GlobalDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 9, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 10, 0, 0), DateTimeKind.Local)
        };

        var actionResult = await controller.QueryOverviewAsync(null, queryRequest, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<GlobalDashboardResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(queryRequest.StartTimeLocal, stubService.LastRequest!.StartTimeLocal);
        Assert.Equal(queryRequest.EndTimeLocal, stubService.LastRequest.EndTimeLocal);
    }
}

