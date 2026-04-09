using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// DangerZoneExecutor 取消语义测试。
/// </summary>
public class DangerZoneExecutorTests
{
    /// <summary>
    /// 调用方主动取消时应输出告警日志而非未预期异常日志。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenCallerCanceled_ShouldLogWarning()
    {
        var logger = new TestLogger<DangerZoneExecutor>();
        var executor = new DangerZoneExecutor(Options.Create(new DangerZoneOptions()), logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = () => executor.ExecuteAsync(
            "cancel-op",
            token => Task.FromCanceled(token),
            cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        Assert.Contains(logger.Logs, log =>
            log.Level == LogLevel.Warning
            && log.Message.Contains("调用方取消请求", StringComparison.Ordinal));
    }

    /// <summary>
    /// 非调用方触发的取消异常仍应按未预期异常记录错误日志。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenUnexpectedCanceled_ShouldLogError()
    {
        var logger = new TestLogger<DangerZoneExecutor>();
        var executor = new DangerZoneExecutor(Options.Create(new DangerZoneOptions()), logger);

        var action = () => executor.ExecuteAsync(
            "unexpected-cancel-op",
            _ => throw new OperationCanceledException("unexpected"));

        await Assert.ThrowsAsync<OperationCanceledException>(action);
        Assert.Contains(logger.Logs, log =>
            log.Level == LogLevel.Error
            && log.Message.Contains("未预期取消异常", StringComparison.Ordinal));
    }
}
