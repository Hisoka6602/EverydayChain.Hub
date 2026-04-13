using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 扫描控制器基础行为测试。
/// </summary>
public sealed class ScanControllerTests {
    /// <summary>
    /// 条码为空时应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodeIsEmpty() {
        var controller = new ScanController(new StubScanIngressService());
        var request = new ScanUploadRequest {
            Barcode = string.Empty,
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 有效请求时应返回 Ok。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnOk_WhenRequestIsValid() {
        var controller = new ScanController(new StubScanIngressService());
        var request = new ScanUploadRequest {
            Barcode = "BC001",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<ScanUploadResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("TASK-001", response.Data.TaskCode);
    }
}
