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
    /// 落格时间语义不是本地或未指定时应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldReturnBadRequest_WhenDropTimeKindIsNonLocal() {
        var controller = new DropFeedbackController(new StubDropFeedbackService());
        var request = new DropFeedbackRequest {
            Barcode = "BC001",
            ActualChuteCode = "CHUTE-01",
            TaskCode = "TASK-001",
            DropTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 13, 12, 0, 0), (DateTimeKind)1)
        };

        var actionResult = await controller.ConfirmAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 有效请求时应返回 Ok。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldReturnOk_WhenRequestIsValid() {
        var stubService = new StubDropFeedbackService();
        var controller = new DropFeedbackController(stubService);
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
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal("TASK-001", stubService.LastRequest!.TaskCode);
    }

    /// <summary>
    /// 任务编码包含首尾空白时应规范化为去空白值。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldTrimTaskCode_WhenTaskCodeHasPadding() {
        var stubService = new StubDropFeedbackService();
        var controller = new DropFeedbackController(stubService);
        var request = new DropFeedbackRequest {
            Barcode = "BC001",
            ActualChuteCode = "CHUTE-01",
            TaskCode = "  TASK-001  ",
            DropTimeLocal = DateTime.Now
        };

        _ = await controller.ConfirmAsync(request, CancellationToken.None);

        Assert.NotNull(stubService.LastRequest);
        Assert.Equal("TASK-001", stubService.LastRequest!.TaskCode);
    }

    /// <summary>
    /// 任务编码全空白时应视为未提供并置为空字符串。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldUseEmptyTaskCode_WhenTaskCodeIsWhitespace() {
        var stubService = new StubDropFeedbackService();
        var controller = new DropFeedbackController(stubService);
        var request = new DropFeedbackRequest {
            Barcode = "BC001",
            ActualChuteCode = "CHUTE-01",
            TaskCode = "   ",
            DropTimeLocal = DateTime.Now
        };

        _ = await controller.ConfirmAsync(request, CancellationToken.None);

        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(string.Empty, stubService.LastRequest!.TaskCode);
    }
}
