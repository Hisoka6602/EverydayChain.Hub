using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 落格回传控制器基础行为测试。
/// </summary>
public sealed class DropFeedbackControllerTests {
    /// <summary>
    /// 实际落格编码为空时应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldReturnBadRequest_WhenActualChuteCodeIsEmpty() {
        var controller = new DropFeedbackController(new StubDropFeedbackService());
        var request = new DropFeedbackRequest {
            Barcode = "BC001",
            ActualChuteCode = string.Empty,
            TaskCode = "TASK-001",
            DropTimeLocal = DateTime.Now
        };

        var actionResult = await controller.ConfirmAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 落格时间为 UTC 时应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldReturnBadRequest_WhenDropTimeIsUtc() {
        var controller = new DropFeedbackController(new StubDropFeedbackService());
        var request = new DropFeedbackRequest {
            Barcode = "BC001",
            ActualChuteCode = "CHUTE-01",
            TaskCode = "TASK-001",
            DropTimeLocal = new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc)
        };

        var actionResult = await controller.ConfirmAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 有效请求时应返回 Ok。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldReturnOk_WhenRequestIsValid() {
        var controller = new DropFeedbackController(new StubDropFeedbackService());
        var request = new DropFeedbackRequest {
            Barcode = "BC001",
            ActualChuteCode = "CHUTE-01",
            TaskCode = "TASK-001",
            DropTimeLocal = DateTime.Now
        };

        var actionResult = await controller.ConfirmAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<DropFeedbackResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("FeedbackPending", response.Data.Status);
    }
}
