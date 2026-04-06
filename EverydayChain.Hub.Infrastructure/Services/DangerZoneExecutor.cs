using EverydayChain.Hub.Domain.Options;
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
        ExecuteWithLoggingAsync(operationName, action, cancellationToken);

    /// <inheritdoc/>
    public Task<T> ExecuteAsync<T>(string operationName, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) =>
        ExecuteWithLoggingAsync(operationName, action, cancellationToken);

    /// <summary>
    /// 执行危险操作并统一记录异常日志。
    /// </summary>
    /// <param name="operationName">操作名称。</param>
    /// <param name="action">执行动作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task ExecuteWithLoggingAsync(string operationName, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await _pipeline.ExecuteAsync(
                static (Func<CancellationToken, Task> callback, CancellationToken token) => new ValueTask(callback(token)),
                action,
                cancellationToken);
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

    /// <summary>
    /// 执行危险操作并统一记录异常日志（带返回值）。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="operationName">操作名称。</param>
    /// <param name="action">执行动作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>执行结果。</returns>
    private async Task<T> ExecuteWithLoggingAsync<T>(string operationName, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            // 使用 Polly 的 state 参数重载传入 action，并以 static lambda + ValueTask 适配签名，避免额外闭包分配。
            return await _pipeline.ExecuteAsync(
                static (Func<CancellationToken, Task<T>> callback, CancellationToken token) => new ValueTask<T>(callback(token)),
                action,
                cancellationToken);
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
