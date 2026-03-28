using EverydayChain.Hub.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 批量写入性能自动调谐器实现，基于采样窗口内的失败率与耗时动态升降批量大小。
/// </summary>
public class SqlExecutionTuner : ISqlExecutionTuner
{
    /// <summary>批量大小读写同步根。</summary>
    private readonly object _syncRoot = new();

    /// <summary>自动调谐配置快照。</summary>
    private readonly AutoTuneOptions _options;

    /// <summary>日志记录器。</summary>
    private readonly ILogger<SqlExecutionTuner> _logger;

    /// <summary>当前推荐批量大小，通过 <see cref="Volatile"/> 保证跨线程可见性。</summary>
    private int _batchSize;

    /// <summary>当前采样窗口内失败次数。</summary>
    private int _failureCount;

    /// <summary>当前采样窗口内总样本数。</summary>
    private int _sampleCount;

    /// <summary>
    /// 初始化调谐器，从配置中读取初始批量大小。
    /// </summary>
    /// <param name="options">自动调谐配置。</param>
    /// <param name="logger">日志记录器。</param>
    public SqlExecutionTuner(IOptions<AutoTuneOptions> options, ILogger<SqlExecutionTuner> logger)
    {
        _options = options.Value;
        _logger = logger;
        _batchSize = _options.InitialBatchSize;
    }

    /// <inheritdoc/>
    public int CurrentBatchSize => Volatile.Read(ref _batchSize);

    /// <inheritdoc/>
    public void Record(TimeSpan elapsed, bool success)
    {
        if (!_options.Enabled)
        {
            return;
        }

        lock (_syncRoot)
        {
            _sampleCount++;
            if (!success)
            {
                _failureCount++;
            }

            // 采样窗口未满时不触发决策。
            if (_sampleCount < _options.SamplingWindowSize)
            {
                return;
            }

            var failureRate = (double)_failureCount / _sampleCount;
            var isSlow = elapsed.TotalMilliseconds > _options.SlowThresholdMilliseconds;
            var old = _batchSize;

            // 失败率超阈值或耗时过长时降低批量大小；否则逐步提升。
            if (failureRate > _options.FailureRateThreshold || isSlow)
            {
                _batchSize = Math.Max(_options.MinBatchSize, _batchSize - _options.DecreaseStep);
            }
            else
            {
                _batchSize = Math.Min(_options.MaxBatchSize, _batchSize + _options.IncreaseStep);
            }

            _logger.LogInformation("自动调谐: 批量大小 {OldBatch} -> {NewBatch}, failureRate={FailureRate}, elapsedMs={ElapsedMs}",
                old, _batchSize, failureRate, elapsed.TotalMilliseconds);

            // 重置采样窗口。
            _sampleCount = 0;
            _failureCount = 0;
        }
    }
}
