using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DockDashboardControllerTests
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task QueryOverviewAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubDockDashboardQueryService();
        var controller = new DockDashboardController(stubService);
        var request = new DockDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "W1"
        };

        var result = await controller.QueryOverviewAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DockDashboardResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal("W1", stubService.LastRequest!.WaveCode);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task QueryOverviewAsync_ShouldReturnBadRequest_WhenTimeRangeInvalid()
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = new DockDashboardController(new StubDockDashboardQueryService());
        var request = new DockDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.QueryOverviewAsync(request, null, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task QueryOverviewAsync_ShouldDefaultEndTimeToStartPlusOneDay_WhenOnlyStartTimeProvided()
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubDockDashboardQueryService();
        var controller = new DockDashboardController(stubService);
        var startTime = DateTime.SpecifyKind(new DateTime(2026, 4, 19, 8, 0, 0), DateTimeKind.Local);
        var request = new DockDashboardQueryRequest
        {
            StartTimeLocal = startTime
        };

        var result = await controller.QueryOverviewAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _ = Assert.IsType<ApiResponse<DockDashboardResponse>>(okResult.Value);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(startTime, stubService.LastRequest!.StartTimeLocal);
        Assert.Equal(startTime.AddDays(1), stubService.LastRequest.EndTimeLocal);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task QueryOverviewAsync_ShouldUseQueryRequest_WhenBodyRequestIsNull()
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubDockDashboardQueryService();
        var controller = new DockDashboardController(stubService);
        var queryRequest = new DockDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "WQ"
        };

        var result = await controller.QueryOverviewAsync(null, queryRequest, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DockDashboardResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal("WQ", stubService.LastRequest!.WaveCode);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ExportCsvAsync_ShouldReturnFile_WhenRequestIsValid()
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubDockDashboardQueryService();
        var controller = new DockDashboardController(stubService);
        var request = new DockDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "W1"
        };

        var result = await controller.ExportCsvAsync(request, null, CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.True(fileResult.FileContents.Length >= 3);
        Assert.Equal(0xEF, fileResult.FileContents[0]);
        Assert.Equal(0xBB, fileResult.FileContents[1]);
        Assert.Equal(0xBF, fileResult.FileContents[2]);
        var csvText = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("DockCode,SplitUnsortedCount,FullCaseUnsortedCount,RecirculatedCount,ExceptionCount,SortedCount,SortedProgressPercent", csvText);
        Assert.Contains("7,1,2,3,1,4,50", csvText);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ExportXlsxAsync_ShouldReturnFile_WhenRequestIsValid()
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubDockDashboardQueryService();
        var controller = new DockDashboardController(stubService);
        var request = new DockDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            WaveCode = "W1"
        };

        var result = await controller.ExportXlsxAsync(request, null, CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.NotEmpty(fileResult.FileContents);
    }
}
