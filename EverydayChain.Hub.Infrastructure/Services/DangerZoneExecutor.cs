using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Collections.Concurrent;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public class DangerZoneExecutor : IDangerZoneExecutor
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly int _defaultTimeoutSeconds;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly DangerZoneOptions _options;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ConcurrentDictionary<int, ResiliencePipeline> _pipelines = [];

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ILogger<DangerZoneExecutor> _logger;

    public DangerZoneExecutor(IOptions<DangerZoneOptions> options, ILogger<DangerZoneExecutor> logger)
    {
        _logger = logger;
        _options = options.Value;
        _defaultTimeoutSeconds = Math.Max(1, _options.TimeoutSeconds);
        _pipelines[_defaultTimeoutSeconds] = BuildPipeline(_defaultTimeoutSeconds);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        int? timeoutSecondsOverride = null) =>
        ExecuteWithLoggingAsync(operationName, action, timeoutSecondsOverride, cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<T> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        int? timeoutSecondsOverride = null) =>
        ExecuteWithLoggingAsync(operationName, action, timeoutSecondsOverride, cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task ExecuteWithLoggingAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        int? timeoutSecondsOverride,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var pipeline = GetPipeline(timeoutSecondsOverride);
        try
        {
            await pipeline.ExecuteAsync(
                static (Func<CancellationToken, Task> callback, CancellationToken token) => new ValueTask(callback(token)),
                action,
                cancellationToken);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "危险操作隔离器检测到调用方取消请求，操作已取消。操作名: {OperationName}", operationName);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "危险操作隔离器执行出现未预期取消异常，操作名: {OperationName}", operationName);
            throw;
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
    /// 执行当前方法。
    /// </summary>
    private async Task<T> ExecuteWithLoggingAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        int? timeoutSecondsOverride,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var pipeline = GetPipeline(timeoutSecondsOverride);
        try
        {
            return await pipeline.ExecuteAsync(
                static (Func<CancellationToken, Task<T>> callback, CancellationToken token) => new ValueTask<T>(callback(token)),
                action,
                cancellationToken);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "危险操作隔离器检测到调用方取消请求，操作已取消。操作名: {OperationName}", operationName);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "危险操作隔离器执行出现未预期取消异常，操作名: {OperationName}", operationName);
            throw;
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

    private ResiliencePipeline GetPipeline(int? timeoutSecondsOverride)
    {
        var timeoutSeconds = Math.Max(1, timeoutSecondsOverride ?? _defaultTimeoutSeconds);
        return _pipelines.GetOrAdd(timeoutSeconds, BuildPipeline);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private ResiliencePipeline BuildPipeline(int timeoutSeconds) =>
        new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(timeoutSeconds))
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not NonRetryableDangerZoneException),
                MaxRetryAttempts = _options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(_options.RetryBaseDelaySeconds)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not NonRetryableDangerZoneException),
                FailureRatio = _options.CircuitBreakerFailureRatio,
                MinimumThroughput = _options.CircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromMinutes(_options.CircuitBreakerSamplingDurationMinutes),
                BreakDuration = TimeSpan.FromSeconds(_options.CircuitBreakerBreakDurationSeconds)
            })
            .Build();
}

