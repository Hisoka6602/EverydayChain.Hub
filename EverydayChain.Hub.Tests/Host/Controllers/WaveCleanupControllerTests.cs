using System.Net;
using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Tests.Host.Controllers;

public sealed class WaveCleanupControllerTests
{
    [Fact]
    public async Task DryRunAsync_ShouldReturnBadRequest_WhenWaveCodeIsEmpty()
    {
        var controller = new WaveCleanupController(new StubWaveCleanupService(), new StubWaveQueryService());
        var request = new WaveCleanupRequest
        {
            WaveCode = string.Empty
        };

        var actionResult = await controller.DryRunAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnBadRequest_WhenWaveCodeIsEmpty()
    {
        var controller = new WaveCleanupController(new StubWaveCleanupService(), new StubWaveQueryService());
        var request = new WaveCleanupRequest
        {
            WaveCode = "  "
        };

        var actionResult = await controller.ExecuteAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task DryRunAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubWaveCleanupService();
        var controller = new WaveCleanupController(stubService, new StubWaveQueryService());
        var request = new WaveCleanupRequest
        {
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

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubWaveCleanupService();
        var controller = new WaveCleanupController(stubService, new StubWaveQueryService());
        var request = new WaveCleanupRequest
        {
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

    [Fact]
    public async Task ExecuteAsync_ShouldCaptureRequestMetadata_WhenHttpContextExists()
    {
        var stubService = new StubWaveCleanupService();
        var controller = new WaveCleanupController(stubService, new StubWaveQueryService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };
        var request = new WaveCleanupRequest
        {
            WaveCode = "WAVE-003"
        };

        var actionResult = await controller.ExecuteAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.IsType<ApiResponse<WaveCleanupResponse>>(okResult.Value);

        Assert.NotNull(stubService.LastExecuteContext);
        Assert.Equal("trace-123", stubService.LastExecuteContext!.TraceId);
        Assert.Equal("/api/v1/wave-cleanup/execute", stubService.LastExecuteContext.RequestPath);
        Assert.Equal(HttpMethods.Post, stubService.LastExecuteContext.HttpMethod);
        Assert.Equal("operator-1", stubService.LastExecuteContext.OperatorId);
        Assert.Equal("127.0.0.1", stubService.LastExecuteContext.ClientIp);
        Assert.Equal("agent/1.0", stubService.LastExecuteContext.UserAgent);
    }

    [Fact]
    public async Task QueryAsync_ShouldReturnOk_WhenWaveExists()
    {
        var controller = new WaveCleanupController(new StubWaveCleanupService(), new StubWaveQueryService());
        var request = new WaveCleanupRequest
        {
            WaveCode = "WAVE-001"
        };

        var actionResult = await controller.QueryAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<WaveCleanupQueryResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Single(response.Data!.Items);
        Assert.Equal("W1", response.Data.Items[0].WaveId);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-123";
        context.Request.Path = "/api/v1/wave-cleanup/execute";
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers["X-Operator-Id"] = " operator-1 ";
        context.Request.Headers.UserAgent = " agent/1.0 ";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        return context;
    }
}
