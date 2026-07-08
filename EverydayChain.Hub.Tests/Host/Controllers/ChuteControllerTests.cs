using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 ChuteControllerTests 类型。
/// </summary>
public sealed class ChuteControllerTests {
    /// <summary>
    /// 执行 ResolveAsync_ShouldReturnBadRequest_WhenBarcodeIsEmpty 方法。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldReturnBadRequest_WhenBarcodeIsEmpty() {
        // 步骤：执行 ResolveAsync_ShouldReturnBadRequest_WhenBarcodeIsEmpty 方法的核心处理流程。
        var controller = new ChuteController(new StubChuteQueryService());
        var request = new ChuteResolveRequest {
            Barcode = string.Empty,
            TaskCode = "TASK-001"
        };

        var actionResult = await controller.ResolveAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 执行 ResolveAsync_ShouldReturnOk_WhenRequestIsValid 方法。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldReturnOk_WhenRequestIsValid() {
        // 步骤：执行 ResolveAsync_ShouldReturnOk_WhenRequestIsValid 方法的核心处理流程。
        var stubService = new StubChuteQueryService();
        var controller = new ChuteController(stubService);
        var request = new ChuteResolveRequest {
            Barcode = "BC001",
            TaskCode = "TASK-001"
        };

        var actionResult = await controller.ResolveAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<ChuteResolveResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("CHUTE-01", response.Data.ChuteCode);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal("TASK-001", stubService.LastRequest!.TaskCode);
    }

    /// <summary>
    /// 执行 ResolveAsync_ShouldTrimTaskCode_WhenTaskCodeHasPadding 方法。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldTrimTaskCode_WhenTaskCodeHasPadding() {
        // 步骤：执行 ResolveAsync_ShouldTrimTaskCode_WhenTaskCodeHasPadding 方法的核心处理流程。
        var stubService = new StubChuteQueryService();
        var controller = new ChuteController(stubService);
        var request = new ChuteResolveRequest {
            Barcode = "BC001",
            TaskCode = "  TASK-001  "
        };

        _ = await controller.ResolveAsync(request, CancellationToken.None);

        Assert.NotNull(stubService.LastRequest);
        Assert.Equal("TASK-001", stubService.LastRequest!.TaskCode);
    }

    /// <summary>
    /// 执行 ResolveAsync_ShouldUseEmptyTaskCode_WhenTaskCodeIsWhitespace 方法。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldUseEmptyTaskCode_WhenTaskCodeIsWhitespace() {
        // 步骤：执行 ResolveAsync_ShouldUseEmptyTaskCode_WhenTaskCodeIsWhitespace 方法的核心处理流程。
        var stubService = new StubChuteQueryService();
        var controller = new ChuteController(stubService);
        var request = new ChuteResolveRequest {
            Barcode = "BC001",
            TaskCode = "   "
        };

        _ = await controller.ResolveAsync(request, CancellationToken.None);

        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(string.Empty, stubService.LastRequest!.TaskCode);
    }
}

