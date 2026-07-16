using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 根据执行耗时与失败率动态调整批量大小。
/// </summary>
public class SqlExecutionTuner : ISqlExecutionTuner
{
    /// <summary>
    /// 保护批量大小和采样计数的并发更新。
    /// </summary>
    private readonly object _syncRoot = new();

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly AutoTuneOptions _options;

    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<SqlExecutionTuner> _logger;

    /// <summary>
    /// 存储 _batchSize 字段。
    /// </summary>
    private int _batchSize;

    /// <summary>
    /// 存储 _failureCount 字段。
    /// </summary>
    private int _failureCount;

    /// <summary>
    /// 存储 _sampleCount 字段。
    /// </summary>
    private int _sampleCount;

    public SqlExecutionTuner(IOptions<AutoTuneOptions> options, ILogger<SqlExecutionTuner> logger)
    {
        _options = options.Value;
        _logger = logger;
        _batchSize = _options.InitialBatchSize;
    }

    /// <summary>
    /// 获取当前建议的批量执行大小。
    /// </summary>
    public int CurrentBatchSize
    {
        get
        {
            lock (_syncRoot)
            {
                return _batchSize;
            }
        }
    }

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

            if (_sampleCount < _options.SamplingWindowSize)
            {
                return;
            }

            var failureRate = EverydayChain.Hub.SharedKernel.Utilities.MetricDecimalUtility.Round(_failureCount / (decimal)_sampleCount);
            var elapsedMilliseconds = EverydayChain.Hub.SharedKernel.Utilities.MetricDecimalUtility.ToMilliseconds(elapsed);
            var isSlow = elapsedMilliseconds > _options.SlowThresholdMilliseconds;
            var old = _batchSize;

            if (failureRate > _options.FailureRateThreshold || isSlow)
            {
                _batchSize = Math.Max(_options.MinBatchSize, _batchSize - _options.DecreaseStep);
            }
            else
            {
                _batchSize = Math.Min(_options.MaxBatchSize, _batchSize + _options.IncreaseStep);
            }

            _logger.LogInformation("自动调谐: 批量大小 {OldBatch} -> {NewBatch}, failureRate={FailureRate}, elapsedMs={ElapsedMs}",
                old, _batchSize, failureRate, elapsedMilliseconds);

            _sampleCount = 0;
            _failureCount = 0;
        }
    }

}

