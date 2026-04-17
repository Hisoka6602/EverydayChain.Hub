using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 波次清理控制器基础行为测试。
/// </summary>
public sealed class WaveCleanupControllerTests {
    /// <summary>
    /// 波次号为空时 dry-run 应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task DryRunAsync_ShouldReturnBadRequest_WhenWaveCodeIsEmpty() {
        var controller = new WaveCleanupController(new StubWaveCleanupService());
        var request = new WaveCleanupRequest {
            WaveCode = string.Empty
        };

        var actionResult = await controller.DryRunAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 波次号为空时正式执行应返回 BadRequest。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnBadRequest_WhenWaveCodeIsEmpty() {
        var controller = new WaveCleanupController(new StubWaveCleanupService());
        var request = new WaveCleanupRequest {
            WaveCode = "  "
        };

        var actionResult = await controller.ExecuteAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// dry-run 请求有效时应返回 Ok 且 IsDryRun 为 true。
    /// </summary>
    [Fact]
    public async Task DryRunAsync_ShouldReturnOk_WhenRequestIsValid() {
        var stubService = new StubWaveCleanupService();
        var controller = new WaveCleanupController(stubService);
        var request = new WaveCleanupRequest {
            WaveCode = " WAVE-001 "
        };

        var actionResult = await controller.DryRunAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<WaveCleanupResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.True(response.Data.IsDryRun);
        Assert.Equal("WAVE-001", stubService.LastDryRunWaveCode);
    }

    /// <summary>
    /// 正式执行请求有效时应返回 Ok 且 IsDryRun 为 false。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnOk_WhenRequestIsValid() {
        var stubService = new StubWaveCleanupService();
        var controller = new WaveCleanupController(stubService);
        var request = new WaveCleanupRequest {
            WaveCode = "WAVE-002"
        };

        var actionResult = await controller.ExecuteAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<WaveCleanupResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.False(response.Data.IsDryRun);
        Assert.Equal("WAVE-002", stubService.LastExecuteWaveCode);
    }
}
