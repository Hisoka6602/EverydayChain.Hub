using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DropFeedbackControllerTests {
    private const DateTimeKind NonLocalKind = (DateTimeKind)1;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldReturnBadRequest_WhenActualChuteCodeIsEmpty() {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldReturnBadRequest_WhenDropTimeKindIsNonLocal() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = new DropFeedbackController(new StubDropFeedbackService());
        var request = new DropFeedbackRequest {
            Barcode = "BC001",
            ActualChuteCode = "CHUTE-01",
            TaskCode = "TASK-001",
            DropTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 13, 12, 0, 0), NonLocalKind)
        };

        var actionResult = await controller.ConfirmAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldReturnOk_WhenRequestIsValid() {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldTrimTaskCode_WhenTaskCodeHasPadding() {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ShouldUseEmptyTaskCode_WhenTaskCodeIsWhitespace() {
        // 步骤：按既定流程执行当前方法逻辑。
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

