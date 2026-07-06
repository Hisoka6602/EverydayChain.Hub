using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveCleanupControllerTests {
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task DryRunAsync_ShouldReturnBadRequest_WhenWaveCodeIsEmpty() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = new WaveCleanupController(new StubWaveCleanupService(), new StubWaveQueryService());
        var request = new WaveCleanupRequest {
            WaveCode = string.Empty
        };

        var actionResult = await controller.DryRunAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnBadRequest_WhenWaveCodeIsEmpty() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = new WaveCleanupController(new StubWaveCleanupService(), new StubWaveQueryService());
        var request = new WaveCleanupRequest {
            WaveCode = "  "
        };

        var actionResult = await controller.ExecuteAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task DryRunAsync_ShouldReturnOk_WhenRequestIsValid() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubWaveCleanupService();
        var controller = new WaveCleanupController(stubService, new StubWaveQueryService());
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
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnOk_WhenRequestIsValid() {
        // 步骤：按既定流程执行当前方法逻辑。
        var stubService = new StubWaveCleanupService();
        var controller = new WaveCleanupController(stubService, new StubWaveQueryService());
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

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldReturnOk_WhenWaveExists() {
        // 步骤：按既定流程执行当前方法逻辑。
        var controller = new WaveCleanupController(new StubWaveCleanupService(), new StubWaveQueryService());
        var request = new WaveCleanupRequest {
            WaveCode = "WAVE-001"
        };

        var actionResult = await controller.QueryAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<WaveCleanupQueryResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Single(response.Data!.Items);
        Assert.Equal("W1", response.Data.Items[0].WaveId);
    }
}

