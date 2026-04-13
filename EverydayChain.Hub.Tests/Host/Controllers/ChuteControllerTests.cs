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
        var controller = new ChuteController(new StubChuteQueryService());
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
    }
}
