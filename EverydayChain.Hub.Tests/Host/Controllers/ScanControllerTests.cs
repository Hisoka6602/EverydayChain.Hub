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
            Barcodes = [],
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
            Barcodes = ["BC001"],
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
            Barcodes = ["BC001"],
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
            Barcodes = ["BC001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("条码类型不受支持。", response.Message);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("UnsupportedBarcodeType", response.Data[0].FailureReason);
    }

    /// <summary>
    /// 有效请求时应返回 Ok。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnOk_WhenRequestIsValid() {
        var controller = new ScanController(new StubScanIngressService());
        var request = new ScanUploadRequest {
            Barcodes = ["BC001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("TASK-001", response.Data[0].TaskCode);
        Assert.Equal("Split", response.Data[0].BarcodeType);
        Assert.Equal(string.Empty, response.Data[0].FailureReason);
    }

    /// <summary>
    /// 条码列表为 null 时应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodesIsNull() {
        var controller = new ScanController(new StubScanIngressService());
        var fixedScanTime = DateTime.SpecifyKind(new DateTime(2026, 4, 14, 10, 0, 0), DateTimeKind.Local);
        var request = new ScanUploadRequest {
            Barcodes = null,
            DeviceCode = "DVC-01",
            ScanTimeLocal = fixedScanTime
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("条码不能为空。", response.Message);
    }

    /// <summary>
    /// 请求体为空时应返回统一错误消息。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenRequestIsNull() {
        var controller = new ScanController(new StubScanIngressService());

        var actionResult = await controller.UploadAsync(null, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("扫描上传请求体不能为空。", response.Message);
    }

    /// <summary>
    /// 条码列表存在空白项时应直接返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodesContainsWhitespaceItem() {
        var controller = new ScanController(new StubScanIngressService());
        var fixedScanTime = DateTime.SpecifyKind(new DateTime(2026, 4, 14, 10, 0, 0), DateTimeKind.Local);
        var request = new ScanUploadRequest {
            Barcodes = ["BC001", " "],
            DeviceCode = "DVC-01",
            ScanTimeLocal = fixedScanTime
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("条码列表中存在空条码，请检查后重试。", response.Message);
    }

    /// <summary>
    /// 超出最大条码数量时应直接返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodesExceedLimit() {
        var controller = new ScanController(new StubScanIngressService());
        var fixedScanTime = DateTime.SpecifyKind(new DateTime(2026, 4, 14, 10, 0, 0), DateTimeKind.Local);
        var request = new ScanUploadRequest {
            Barcodes = Enumerable.Repeat("BC001", 101).ToList(),
            DeviceCode = "DVC-01",
            ScanTimeLocal = fixedScanTime
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("单次最多允许提交 100 个条码。", response.Message);
    }

    /// <summary>
    /// 多条码请求时非首条条码应使用 0 作为尺寸与重量回写值。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldApplyZeroMeasurement_ForNonPrimaryBarcodes() {
        var stubService = new StubScanIngressService();
        var controller = new ScanController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["BC001", "BC002", "BC003"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now,
            LengthMm = 100,
            WidthMm = 200,
            HeightMm = 300,
            VolumeMm3 = 6000000,
            WeightGram = 400
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        _ = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(okResult.Value);

        Assert.Equal(3, stubService.Requests.Count);
        Assert.Equal(100, stubService.Requests[0].LengthMm);
        Assert.Equal(200, stubService.Requests[0].WidthMm);
        Assert.Equal(300, stubService.Requests[0].HeightMm);
        Assert.Equal(6000000, stubService.Requests[0].VolumeMm3);
        Assert.Equal(400, stubService.Requests[0].WeightGram);

        Assert.Equal(0, stubService.Requests[1].LengthMm);
        Assert.Equal(0, stubService.Requests[1].WidthMm);
        Assert.Equal(0, stubService.Requests[1].HeightMm);
        Assert.Equal(0, stubService.Requests[1].VolumeMm3);
        Assert.Equal(0, stubService.Requests[1].WeightGram);
        Assert.Equal(stubService.Requests[0].ScanTimeLocal, stubService.Requests[1].ScanTimeLocal);

        Assert.Equal(0, stubService.Requests[2].LengthMm);
        Assert.Equal(0, stubService.Requests[2].WidthMm);
        Assert.Equal(0, stubService.Requests[2].HeightMm);
        Assert.Equal(0, stubService.Requests[2].VolumeMm3);
        Assert.Equal(0, stubService.Requests[2].WeightGram);
        Assert.Equal(stubService.Requests[0].ScanTimeLocal, stubService.Requests[2].ScanTimeLocal);
    }
}
