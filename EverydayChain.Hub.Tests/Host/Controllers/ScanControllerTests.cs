using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 扫描控制器基础行为测试。
/// </summary>
public sealed class ScanControllerTests {
    /// <summary>
    /// 非本地时间语义枚举值。
    /// </summary>
    private const DateTimeKind NonLocalKind = (DateTimeKind)1;

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
    /// 扫描时间语义不是本地或未指定时应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenScanTimeKindIsNonLocal() {
        var controller = new ScanController(new StubScanIngressService());
        var request = new ScanUploadRequest {
            Barcode = "BC001",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 13, 12, 0, 0), NonLocalKind)
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// TraceId 为 null 时应回退为空字符串并避免空引用异常。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldFallbackEmptyTraceId_WhenTraceIdIsNull() {
        var stubService = new StubScanIngressService();
        var controller = new ScanController(stubService);
#pragma warning disable CS8625
        var request = new ScanUploadRequest {
            Barcode = "BC001",
            DeviceCode = "DVC-01",
            TraceId = null,
            ScanTimeLocal = DateTime.Now
        };
#pragma warning restore CS8625

        _ = await controller.UploadAsync(request, CancellationToken.None);

        Assert.NotNull(stubService.LastRequest);
        Assert.Equal(string.Empty, stubService.LastRequest!.TraceId);
    }

    /// <summary>
    /// 解析失败时应返回可读失败消息并保留失败代码。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnReadableFailureMessage_WhenApplicationRejected() {
        var stubService = new StubScanIngressService {
            Result = new ScanUploadApplicationResult {
                IsAccepted = false,
                TaskCode = string.Empty,
                BarcodeType = "Unknown",
                FailureReason = "UnsupportedBarcodeType",
                Message = "条码类型不受支持。"
            }
        };
        var controller = new ScanController(stubService);
        var request = new ScanUploadRequest {
            Barcode = "BC001",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<ScanUploadResponse>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("条码类型不受支持。", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal("UnsupportedBarcodeType", response.Data.FailureReason);
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
        Assert.Equal("Split", response.Data.BarcodeType);
        Assert.Equal(string.Empty, response.Data.FailureReason);
    }
}
