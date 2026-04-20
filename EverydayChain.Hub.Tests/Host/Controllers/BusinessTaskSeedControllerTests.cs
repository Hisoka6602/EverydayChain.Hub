using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 业务任务模拟补数控制器测试。
/// </summary>
public sealed class BusinessTaskSeedControllerTests
{
    /// <summary>
    /// 空请求体应返回中文 400 错误。
    /// </summary>
    [Fact]
    public async Task ManualSeedAsync_ShouldReturnBadRequest_WhenRequestBodyIsNull()
    {
        var controller = new BusinessTaskSeedController(new StubBusinessTaskSeedService(), NullLogger<BusinessTaskSeedController>.Instance);

        var actionResult = await controller.ManualSeedAsync(null, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<BusinessTaskSeedResponse>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("模拟补数请求体不能为空。", response.Message);
    }

    /// <summary>
    /// 业务校验失败时应返回 400。
    /// </summary>
    [Fact]
    public async Task ManualSeedAsync_ShouldReturnBadRequest_WhenServiceValidationFailed()
    {
        var stubService = new StubBusinessTaskSeedService
        {
            Result = new()
            {
                IsSuccess = false,
                Message = "目标表名非法，仅允许 business_tasks_yyyyMM 格式。",
                TargetTableName = "invalid_table",
                InsertedBarcodes = [],
                SkippedBarcodes = ["BC001"]
            }
        };
        var controller = new BusinessTaskSeedController(stubService, NullLogger<BusinessTaskSeedController>.Instance);

        var actionResult = await controller.ManualSeedAsync(new BusinessTaskSeedRequest
        {
            TargetTableName = "invalid_table",
            Barcodes = ["BC001"]
        }, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<BusinessTaskSeedResponse>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("目标表名非法，仅允许 business_tasks_yyyyMM 格式。", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal("invalid_table", response.Data.TargetTableName);
        Assert.NotNull(response.Data.InsertedBarcodes);
        Assert.NotNull(response.Data.SkippedBarcodes);
    }

    /// <summary>
    /// 成功请求应返回 200。
    /// </summary>
    [Fact]
    public async Task ManualSeedAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubBusinessTaskSeedService
        {
            Result = new()
            {
                IsSuccess = true,
                Message = "模拟补数写入成功。",
                TargetTableName = "business_tasks_202604",
                RequestedCount = 2,
                FilteredEmptyCount = 0,
                DeduplicatedCount = 0,
                CandidateCount = 2,
                InsertedCount = 2,
                SkippedExistingCount = 0,
                InsertedBarcodes = ["BC001", "BC002"],
                SkippedBarcodes = []
            }
        };
        var controller = new BusinessTaskSeedController(stubService, NullLogger<BusinessTaskSeedController>.Instance);

        var actionResult = await controller.ManualSeedAsync(new BusinessTaskSeedRequest
        {
            TargetTableName = "business_tasks_202604",
            Barcodes = ["BC001", "BC002"]
        }, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<BusinessTaskSeedResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("模拟补数写入成功。", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.InsertedCount);
        Assert.Equal(["BC001", "BC002"], response.Data.InsertedBarcodes);
    }

    /// <summary>
    /// 返回结构应保持 ApiResponse 包装。
    /// </summary>
    [Fact]
    public async Task ManualSeedAsync_ShouldReturnApiResponseStructure()
    {
        var stubService = new StubBusinessTaskSeedService();
        var controller = new BusinessTaskSeedController(stubService, NullLogger<BusinessTaskSeedController>.Instance);

        var actionResult = await controller.ManualSeedAsync(new BusinessTaskSeedRequest
        {
            TargetTableName = "business_tasks_202604",
            Barcodes = ["BC001"]
        }, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<BusinessTaskSeedResponse>>(okResult.Value);

        Assert.NotNull(response.Data);
        Assert.Equal("business_tasks_202604", response.Data.TargetTableName);
        Assert.Single(stubService.LastCommand!.Barcodes);
    }
}
