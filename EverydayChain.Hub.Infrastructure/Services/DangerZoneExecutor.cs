using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 危险操作隔离器实现，为高风险操作统一提供超时（30s）、指数退避重试（最多 2 次）
/// 及熔断（50% 失败率触发，熔断 20s）保护。
/// </summary>
public class DangerZoneExecutor(ILogger<DangerZoneExecutor> logger) : IDangerZoneExecutor
{
    /// <summary>Polly 弹性管道：超时 → 重试 → 熔断。</summary>
    private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddRetry(new() { MaxRetryAttempts = 2, BackoffType = DelayBackoffType.Exponential, Delay = TimeSpan.FromSeconds(1) })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromMinutes(1),
            BreakDuration = TimeSpan.FromSeconds(20)
        })
        .Build();

    /// <inheritdoc/>
    public Task ExecuteAsync(string operationName, Func<CancellationToken, Task> action, CancellationToken cancellationToken) =>
        ExecuteAsync<object?>(operationName, async token =>
        {
            await action(token);
            return null;
        }, cancellationToken);

    /// <inheritdoc/>
    public async Task<T> ExecuteAsync<T>(string operationName, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async token => await action(token), cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogError(ex, "危险操作隔离器触发熔断，操作名: {OperationName}", operationName);
            throw;
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogError(ex, "危险操作隔离器触发超时，操作名: {OperationName}", operationName);
            throw;
        }
    }
}
