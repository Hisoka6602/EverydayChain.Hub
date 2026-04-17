using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Text;

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
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.True(fileResult.FileContents.Length >= 3);
        Assert.Equal(0xEF, fileResult.FileContents[0]);
        Assert.Equal(0xBB, fileResult.FileContents[1]);
        Assert.Equal(0xBF, fileResult.FileContents[2]);
        var csvText = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("码头号,拆零总数,整件总数,拆零分拣数,整件分拣数,回流数,异常数", csvText);
        Assert.Contains("7,1", csvText);
    }
}
