using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 分拣报表控制器测试。
/// </summary>
public sealed class SortingReportControllerTests
{
    /// <summary>
    /// 查询接口有效请求应返回成功结果。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubSortingReportQueryService();
        var controller = new SortingReportController(stubService);
        var request = new SortingReportQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.QueryAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SortingReportResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
    }

    /// <summary>
    /// 导出接口应返回 CSV 文件。
    /// </summary>
    [Fact]
    public async Task ExportCsvAsync_ShouldReturnFile_WhenRequestIsValid()
    {
        var stubService = new StubSortingReportQueryService();
        var controller = new SortingReportController(stubService);
        var request = new SortingReportQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.ExportCsvAsync(request, CancellationToken.None);
        Assert.IsType<FileContentResult>(result);
    }
}
