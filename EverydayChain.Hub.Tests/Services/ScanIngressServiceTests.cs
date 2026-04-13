using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 扫描上传应用服务测试。
/// </summary>
public sealed class ScanIngressServiceTests
{
    /// <summary>
    /// 无效条码应返回统一失败语义。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnInvalidBarcode_WhenBarcodeIsInvalid()
    {
        var service = new ScanIngressService(new BarcodeParser());
        var request = new ScanUploadApplicationRequest
        {
            Barcode = " ",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsAccepted);
        Assert.Equal("Unknown", result.BarcodeType);
        Assert.Equal("InvalidBarcode", result.FailureReason);
    }

    /// <summary>
    /// 有效拆零条码应返回受理结果与条码类型。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldAcceptRequest_WhenBarcodeIsSplit()
    {
        var service = new ScanIngressService(new BarcodeParser());
        var request = new ScanUploadApplicationRequest
        {
            Barcode = "S-ABC001",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsAccepted);
        Assert.Equal("Split", result.BarcodeType);
        Assert.Equal(string.Empty, result.FailureReason);
        Assert.Equal("DVC-01", result.TaskCode[..6]);
    }
}
