using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ScanControllerTests {
    private const DateTimeKind NonLocalKind = (DateTimeKind)1;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static ScanController CreateController(StubScanIngressService stubService) {
        // 步骤：按既定流程执行当前方法逻辑。
        return new ScanController(stubService, new BarcodeParser());
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodeIsEmpty() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = CreateController(new StubScanIngressService());
        var request = new ScanUploadRequest {
            Barcodes = [],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenScanTimeKindIsNonLocal() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = CreateController(new StubScanIngressService());
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 13, 12, 0, 0), NonLocalKind)
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldFallbackEmptyTraceId_WhenTraceIdIsNull() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService();
        var controller = CreateController(stubService);
#pragma warning disable CS8625
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001"],
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnReadableFailureMessage_WhenApplicationRejected() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService {
            Result = new ScanUploadApplicationResult {
                IsAccepted = false,
                TaskCode = string.Empty,
                BarcodeType = "Unknown",
                FailureReason = "UnsupportedBarcodeType",
                Message = "条码类型不受支持。"
            }
        };
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001"],
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnPerItemResult_WhenSingleBarcodeIsUnresolvable() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService {
            Result = new ScanUploadApplicationResult {
                IsAccepted = false,
                TaskCode = string.Empty,
                BarcodeType = "Unknown",
                FailureReason = "UnsupportedBarcodeType",
                Message = "条码类型不受支持。"
            }
        };
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["AB560419318300100001"],
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
        Assert.Single(stubService.Requests);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnOk_WhenRequestIsValid() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = CreateController(new StubScanIngressService());
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001"],
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodesIsNull() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = CreateController(new StubScanIngressService());
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenRequestIsNull() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = CreateController(new StubScanIngressService());

        var actionResult = await controller.UploadAsync(null, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("扫描上传请求体不能为空。", response.Message);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodesContainsWhitespaceItem() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = CreateController(new StubScanIngressService());
        var fixedScanTime = DateTime.SpecifyKind(new DateTime(2026, 4, 14, 10, 0, 0), DateTimeKind.Local);
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001", " "],
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodesExceedLimit() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = CreateController(new StubScanIngressService());
        var fixedScanTime = DateTime.SpecifyKind(new DateTime(2026, 4, 14, 10, 0, 0), DateTimeKind.Local);
        var request = new ScanUploadRequest {
            Barcodes = Enumerable.Repeat("Z130419305700070001", 101).ToList(),
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldApplyZeroMeasurement_ForNonPrimaryBarcodes() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService();
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001", "Z160419318300100001", "Z190419318300100001"],
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

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldSucceed_WhenSingleZBarcodeIsValid() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService();
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Single(stubService.Requests);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldSucceed_WhenMultipleZBarcodesShareSameChute() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService();
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001", "Z160419318300100001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(2, stubService.Requests.Count);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenZBarcodesContainMultipleChutes() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService();
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001", "Z560419318300100001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("扫描 barcodes 不能包含多个格口的条码。", response.Message);
        Assert.Empty(stubService.Requests);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenBarcodesContainUnresolvableChute() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService();
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["Z130419305700070001", "AB560419318300100001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("扫描 barcodes 内不能包含无法解析格口的条码。", response.Message);
        Assert.Empty(stubService.Requests);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldSucceed_WhenMixed02AndZBarcodesShareSameChute() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService();
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["02123456", "Z130419305700070001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(2, stubService.Requests.Count);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnBadRequest_WhenMixed02AndZBarcodesContainDifferentChutes() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubScanIngressService();
        var controller = CreateController(stubService);
        var request = new ScanUploadRequest {
            Barcodes = ["02123456", "Z560419318300100001"],
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var actionResult = await controller.UploadAsync(request, CancellationToken.None);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ScanUploadResponse>>>(badRequestResult.Value);

        Assert.False(response.IsSuccess);
        Assert.Equal("扫描 barcodes 不能包含多个格口的条码。", response.Message);
        Assert.Empty(stubService.Requests);
    }
}

