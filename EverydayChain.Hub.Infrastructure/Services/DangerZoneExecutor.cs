using EverydayChain.Hub.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 危险操作隔离器实现，为高风险操作统一提供超时、指数退避重试及熔断保护。
/// 所有弹性参数均来自 <see cref="DangerZoneOptions"/>，可通过 appsettings.json 的
/// <c>DangerZone</c> 节点进行调整，无需重新编译。
/// </summary>
public class DangerZoneExecutor : IDangerZoneExecutor
{
    /// <summary>Polly 弹性管道：超时 → 重试 → 熔断。</summary>
    private readonly ResiliencePipeline _pipeline;

    /// <summary>日志记录器。</summary>
    private readonly ILogger<DangerZoneExecutor> _logger;

    /// <summary>
    /// 初始化隔离器，根据配置构建 Polly 弹性管道。
    /// </summary>
    /// <param name="options">弹性策略配置。</param>
    /// <param name="logger">日志记录器。</param>
    public DangerZoneExecutor(IOptions<DangerZoneOptions> options, ILogger<DangerZoneExecutor> logger)
    {
        _logger = logger;
        var opt = options.Value;
        _pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(opt.TimeoutSeconds))
            .AddRetry(new()
            {
                MaxRetryAttempts = opt.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(opt.RetryBaseDelaySeconds)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = opt.CircuitBreakerFailureRatio,
                MinimumThroughput = opt.CircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromMinutes(opt.CircuitBreakerSamplingDurationMinutes),
                BreakDuration = TimeSpan.FromSeconds(opt.CircuitBreakerBreakDurationSeconds)
            })
            .Build();
    }

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
            _logger.LogError(ex, "危险操作隔离器触发熔断，操作名: {OperationName}", operationName);
            throw;
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "危险操作隔离器触发超时，操作名: {OperationName}", operationName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "危险操作隔离器执行出现未预期异常，操作名: {OperationName}", operationName);
            throw;
        }
    }
}
