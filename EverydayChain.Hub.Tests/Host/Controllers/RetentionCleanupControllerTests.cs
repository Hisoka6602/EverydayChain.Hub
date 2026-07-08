using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using RetentionCleanupAppItem = EverydayChain.Hub.Application.Models.RetentionCleanupAuditItem;
using RetentionCleanupAppQueryRequest = EverydayChain.Hub.Application.Models.RetentionCleanupAuditQueryRequest;
using RetentionCleanupAppQueryResult = EverydayChain.Hub.Application.Models.RetentionCleanupAuditQueryResult;
using RetentionCleanupApiQueryRequest = EverydayChain.Hub.Host.Contracts.Requests.RetentionCleanupAuditQueryRequest;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义保留期清理控制器测试。
/// </summary>
public sealed class RetentionCleanupControllerTests
{
    /// <summary>
    /// 当时间范围缺失时应返回错误。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldReturnBadRequest_WhenTimeRangeIsMissing()
    {
        // 步骤：构造空时间范围请求并验证控制器会在入口直接拒绝该查询。
        var controller = new RetentionCleanupController(new StubRetentionCleanupQueryService());
        var request = new RetentionCleanupApiQueryRequest();

        var actionResult = await controller.QueryAsync(request, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 当查询条件有效时应返回分页结果。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        // 步骤：传入有效时间范围与分页参数，验证控制器会调用查询服务并返回映射后的响应体。
        var stubQueryService = new StubRetentionCleanupQueryService();
        var controller = new RetentionCleanupController(stubQueryService);
        var request = new RetentionCleanupApiQueryRequest
        {
            StartTimeLocal = new DateTime(2026, 7, 1, 0, 0, 0),
            EndTimeLocal = new DateTime(2026, 7, 7, 23, 59, 59),
            LogicalTableName = "scan_logs",
            PageNumber = 2,
            PageSize = 20
        };

        var actionResult = await controller.QueryAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<RetentionCleanupAuditQueryResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.PageNumber);
        Assert.Equal(20, response.Data.PageSize);
        Assert.Single(response.Data.Items);
        Assert.Equal("scan_logs", response.Data.Items[0].LogicalTableName);
        Assert.NotNull(stubQueryService.LastRequest);
        Assert.Equal("scan_logs", stubQueryService.LastRequest!.LogicalTableName);
    }

    /// <summary>
    /// 提供保留期清理查询服务测试桩。
    /// </summary>
    private sealed class StubRetentionCleanupQueryService : IRetentionCleanupQueryService
    {
        /// <summary>
        /// 获取最近一次收到的请求。
        /// </summary>
        public RetentionCleanupAppQueryRequest? LastRequest { get; private set; }

        /// <summary>
        /// 返回预设的分页查询结果。
        /// </summary>
        /// <param name="request">查询条件。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>测试使用的固定查询结果。</returns>
        public Task<RetentionCleanupAppQueryResult> QueryAsync(RetentionCleanupAppQueryRequest request, CancellationToken cancellationToken)
        {
            // 步骤：记录控制器传入的参数，并返回一条固定审计明细供映射断言使用。
            LastRequest = request;
            return Task.FromResult(new RetentionCleanupAppQueryResult
            {
                TotalCount = 1,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Items =
                [
                    new RetentionCleanupAppItem
                    {
                        Id = "AUDIT-001",
                        BatchId = "BATCH-001",
                        TargetCode = "scan_logs-retention",
                        LogicalTableName = "scan_logs",
                        RetentionMode = "DropShards",
                        TimeColumnName = string.Empty,
                        KeepMonths = 3,
                        IsDryRun = false,
                        AllowDelete = true,
                        ExecutionStage = "Completed",
                        ScannedCount = 4,
                        CandidateCount = 2,
                        DeletedCount = 2,
                        Message = "已删除两张过期分表。",
                        InstanceId = "host-a-1",
                        ThresholdTimeLocal = new DateTime(2026, 4, 1, 0, 0, 0),
                        StartedTimeLocal = new DateTime(2026, 7, 7, 1, 0, 0),
                        CompletedTimeLocal = new DateTime(2026, 7, 7, 1, 0, 3)
                    }
                ]
            });
        }
    }
}
