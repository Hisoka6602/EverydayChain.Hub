using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 业务任务查询控制器测试。
/// </summary>
public sealed class BusinessTaskQueryControllerTests
{
    /// <summary>
    /// 任务查询有效请求应返回成功结果。
    /// </summary>
    [Fact]
    public async Task QueryTasksAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubBusinessTaskReadService();
        var controller = new BusinessTaskQueryController(stubService);
        var request = new BusinessTaskQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            PageNumber = 1,
            PageSize = 50
        };

        var result = await controller.QueryTasksAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<BusinessTaskQueryResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastRequest);
    }

    /// <summary>
    /// 非法分页参数应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task QueryTasksAsync_ShouldReturnBadRequest_WhenPageSizeInvalid()
    {
        var controller = new BusinessTaskQueryController(new StubBusinessTaskReadService());
        var request = new BusinessTaskQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            PageNumber = 1,
            PageSize = 0
        };

        var result = await controller.QueryTasksAsync(request, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
