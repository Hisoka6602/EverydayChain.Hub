using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Http;
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

        var result = await controller.QueryTasksAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<BusinessTaskQueryResponse>>(okResult.Value);
        Assert.NotNull(response.Data);
        var responseData = response.Data!;

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(stubService.Result.HasMore, responseData.HasMore);
        Assert.Equal(stubService.Result.NextLastCreatedTimeLocal, responseData.NextLastCreatedTimeLocal);
        Assert.Equal(stubService.Result.NextLastId, responseData.NextLastId);
        Assert.Equal(stubService.Result.PaginationMode, responseData.PaginationMode);
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

        var result = await controller.QueryTasksAsync(request, null, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// 游标参数不成对时应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task QueryTasksAsync_ShouldReturnBadRequest_WhenCursorParameterPairIsInvalid()
    {
        var controller = new BusinessTaskQueryController(new StubBusinessTaskReadService());
        var request = new BusinessTaskQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            PageNumber = 1,
            PageSize = 50,
            LastCreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 10, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.QueryTasksAsync(request, null, CancellationToken.None);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<BusinessTaskQueryResponse>>(badRequest.Value);
        Assert.Contains("LastCreatedTimeLocal 与 LastId 必须同时传入或同时为空", apiResponse.Message);
    }

    /// <summary>
    /// 游标主键非法时应返回 BadRequest。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task QueryTasksAsync_ShouldReturnBadRequest_WhenLastIdIsNotPositive(long lastId)
    {
        var controller = new BusinessTaskQueryController(new StubBusinessTaskReadService());
        var request = new BusinessTaskQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            PageNumber = 1,
            PageSize = 50,
            LastCreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 10, 0, 0), DateTimeKind.Local),
            LastId = lastId
        };

        var result = await controller.QueryTasksAsync(request, null, CancellationToken.None);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<BusinessTaskQueryResponse>>(badRequest.Value);
        Assert.Contains("LastId 必须大于 0", apiResponse.Message);
    }

    /// <summary>
    /// 请求体为空时任务查询应回退使用查询字符串请求。
    /// </summary>
    [Fact]
    public async Task QueryTasksAsync_ShouldUseQueryRequest_WhenBodyRequestIsNull()
    {
        var stubService = new StubBusinessTaskReadService();
        var controller = new BusinessTaskQueryController(stubService);
        var queryRequest = new BusinessTaskQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
            PageNumber = 1,
            PageSize = 50
        };

        var result = await controller.QueryTasksAsync(null, queryRequest, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<BusinessTaskQueryResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(queryRequest.StartTimeLocal, stubService.LastRequest!.StartTimeLocal);
        Assert.Equal(queryRequest.EndTimeLocal, stubService.LastRequest.EndTimeLocal);
    }
}
