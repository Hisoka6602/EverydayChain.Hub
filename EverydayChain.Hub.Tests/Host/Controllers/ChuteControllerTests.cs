using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 格口控制器基础行为测试。
/// </summary>
public sealed class ChuteControllerTests {
    /// <summary>
    /// 条码为空时应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldReturnBadRequest_WhenBarcodeIsEmpty() {
        var controller = new ChuteController(new StubChuteQueryService());
        var request = new ChuteResolveRequest {
            Barcode = string.Empty,
            TaskCode = "TASK-001"
        };

        var actionResult = await controller.ResolveAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 有效请求时应返回 Ok。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldReturnOk_WhenRequestIsValid() {
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
    /// 任务编码包含首尾空白时应规范化为去空白值。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldTrimTaskCode_WhenTaskCodeHasPadding() {
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
    /// 任务编码全空白时应视为未提供并置为空字符串。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldUseEmptyTaskCode_WhenTaskCodeIsWhitespace() {
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
