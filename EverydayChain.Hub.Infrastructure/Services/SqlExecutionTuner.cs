using EverydayChain.Hub.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

public class SqlExecutionTuner : ISqlExecutionTuner {
    private readonly object _syncRoot = new();
    private readonly AutoTuneOptions _options;
    private readonly ILogger<SqlExecutionTuner> _logger;
    private int _batchSize;
    private int _failureCount;
    private int _sampleCount;

    public SqlExecutionTuner(IOptions<AutoTuneOptions> options, ILogger<SqlExecutionTuner> logger) {
        _options = options.Value;
        _logger = logger;
        _batchSize = _options.InitialBatchSize;
    }

    public int CurrentBatchSize => Volatile.Read(ref _batchSize);

    public void Record(TimeSpan elapsed, bool success) {
        if (!_options.Enabled) {
            return;
        }

        lock (_syncRoot) {
            _sampleCount++;
            if (!success) {
                _failureCount++;
            }

            if (_sampleCount < 10) {
                return;
            }

            var failureRate = (double)_failureCount / _sampleCount;
            var isSlow = elapsed.TotalMilliseconds > _options.SlowThresholdMilliseconds;
            var old = _batchSize;

            if (failureRate > 0.2 || isSlow) {
                _batchSize = Math.Max(_options.MinBatchSize, _batchSize - _options.DecreaseStep);
            }
            else {
                _batchSize = Math.Min(_options.MaxBatchSize, _batchSize + _options.IncreaseStep);
            }

            _logger.LogInformation("自动调谐: 批量大小 {OldBatch} -> {NewBatch}, failureRate={FailureRate}, elapsedMs={ElapsedMs}",
                old, _batchSize, failureRate, elapsed.TotalMilliseconds);

            _sampleCount = 0;
            _failureCount = 0;
        }
    }
}
