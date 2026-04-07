using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Collections.Concurrent;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 危险操作隔离器实现，为高风险操作统一提供超时、指数退避重试及熔断保护。
/// 所有弹性参数均来自 <see cref="DangerZoneOptions"/>，可通过 appsettings.json 的
/// <c>DangerZone</c> 节点进行调整，无需重新编译。
/// </summary>
public class DangerZoneExecutor : IDangerZoneExecutor
{
    /// <summary>默认超时（秒）。</summary>
    private readonly int _defaultTimeoutSeconds;

    /// <summary>弹性参数快照。</summary>
    private readonly DangerZoneOptions _options;

    /// <summary>
    /// Polly 弹性管道缓存（key=超时秒数）。
    /// 注意：每个超时值对应独立的管道实例，熔断器统计与开路状态按超时值分片，不同超时值的操作不共享熔断计数。
    /// 实践中覆盖超时仅用于启动迁移（单次执行），对全局熔断保护影响可忽略。
    /// </summary>
    private readonly ConcurrentDictionary<int, ResiliencePipeline> _pipelines = [];

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
        _options = options.Value;
        _defaultTimeoutSeconds = Math.Max(1, _options.TimeoutSeconds);
        _pipelines[_defaultTimeoutSeconds] = BuildPipeline(_defaultTimeoutSeconds);
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null) =>
        ExecuteWithLoggingAsync(operationName, action, timeoutSecondsOverride, cancellationToken);

    /// <inheritdoc/>
    public Task<T> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null) =>
        ExecuteWithLoggingAsync(operationName, action, timeoutSecondsOverride, cancellationToken);

    /// <summary>
    /// 执行危险操作并统一记录异常日志。
    /// </summary>
    /// <param name="operationName">操作名称。</param>
    /// <param name="action">执行动作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task ExecuteWithLoggingAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        int? timeoutSecondsOverride,
        CancellationToken cancellationToken)
    {
        var pipeline = GetPipeline(timeoutSecondsOverride);
        try
        {
            await pipeline.ExecuteAsync(
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
    private async Task<T> ExecuteWithLoggingAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        int? timeoutSecondsOverride,
        CancellationToken cancellationToken)
    {
        var pipeline = GetPipeline(timeoutSecondsOverride);
        try
        {
            // 使用 Polly 的 state 参数重载传入 action，并以 static lambda + ValueTask 适配签名，避免额外闭包分配。
            return await pipeline.ExecuteAsync(
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

    /// <summary>按超时值获取弹性管道，若不存在则动态构建并缓存。</summary>
    private ResiliencePipeline GetPipeline(int? timeoutSecondsOverride)
    {
        var timeoutSeconds = Math.Max(1, timeoutSecondsOverride ?? _defaultTimeoutSeconds);
        return _pipelines.GetOrAdd(timeoutSeconds, BuildPipeline);
    }

    /// <summary>构建指定超时值的弹性管道。</summary>
    private ResiliencePipeline BuildPipeline(int timeoutSeconds) =>
        new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(timeoutSeconds))
            .AddRetry(new()
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(_options.RetryBaseDelaySeconds)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = _options.CircuitBreakerFailureRatio,
                MinimumThroughput = _options.CircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromMinutes(_options.CircuitBreakerSamplingDurationMinutes),
                BreakDuration = TimeSpan.FromSeconds(_options.CircuitBreakerBreakDurationSeconds)
            })
            .Build();
}
