using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定期清理超过保留期的日志文件。
/// </summary>
public sealed class LogCleanupHostedService : BackgroundService
{
    /// <summary>
    /// 存储 MinimumCheckIntervalHours 常量。
    /// </summary>
    private const int MinimumCheckIntervalHours = 1;

    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<LogCleanupHostedService> _logger;

    /// <summary>
    /// 存储 _maintenanceService 字段。
    /// </summary>
    private readonly LogFileMaintenanceService _maintenanceService;

    /// <summary>
    /// 存储 _optionsLock 字段。
    /// </summary>
    private readonly object _optionsLock = new();

    /// <summary>
    /// 存储 _changeRegistration 字段。
    /// </summary>
    private readonly IDisposable? _changeRegistration;

    /// <summary>
    /// 存储 _currentOptions 字段。
    /// </summary>
    private LogCleanupOptions _currentOptions;

    /// <summary>
    /// 初始化日志清理后台服务。
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="optionsMonitor">配置监听器。</param>
    /// <param name="maintenanceService">日志维护服务。</param>
    public LogCleanupHostedService(
        ILogger<LogCleanupHostedService> logger,
        IOptionsMonitor<LogCleanupOptions> optionsMonitor,
        LogFileMaintenanceService maintenanceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));

        ArgumentNullException.ThrowIfNull(optionsMonitor);
        _currentOptions = Normalize(optionsMonitor.CurrentValue, shouldLog: false);
        _changeRegistration = optionsMonitor.OnChange(options =>
        {
            var normalized = Normalize(options, shouldLog: true);
            lock (_optionsLock)
            {
                _currentOptions = normalized;
            }
        });
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hasLoggedDisabled = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            var currentOptions = GetCurrentOptions();
            if (!currentOptions.Enabled)
            {
                if (!hasLoggedDisabled)
                {
                    _logger.LogInformation("日志清理后台服务已禁用。");
                    hasLoggedDisabled = true;
                }
            }
            else
            {
                hasLoggedDisabled = false;
                try
                {
                    await _maintenanceService.CleanupOldLogsAsync(currentOptions, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "执行日志清理时发生异常。");
                }
            }

            var delayHours = Math.Max(GetCurrentOptions().CheckIntervalHours, MinimumCheckIntervalHours);
            try
            {
                await Task.Delay(TimeSpan.FromHours(delayHours), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _changeRegistration?.Dispose();
        base.Dispose();
    }

    private LogCleanupOptions GetCurrentOptions()
    {
        lock (_optionsLock)
        {
            return _currentOptions;
        }
    }

    private LogCleanupOptions Normalize(LogCleanupOptions? options, bool shouldLog)
    {
        var candidate = options ?? new LogCleanupOptions();
        var validationErrors = candidate.Validate();
        if (validationErrors.Count == 0)
        {
            return candidate;
        }

        if (shouldLog)
        {
            _logger.LogWarning(
                "检测到无效的 LogCleanup 配置热更新，已继续沿用上一份有效配置。Errors={Errors}",
                string.Join(" | ", validationErrors));
        }

        lock (_optionsLock)
        {
            return _currentOptions ?? new LogCleanupOptions();
        }
    }
}
