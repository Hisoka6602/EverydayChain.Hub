using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 启动前检查宿主所在磁盘剩余空间，不足时优先尝试清理日志。
/// </summary>
public sealed class LogCleanupStartupGuardHostedService : IHostedService
{
    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<LogCleanupStartupGuardHostedService> _logger;

    /// <summary>
    /// 存储 _maintenanceService 字段。
    /// </summary>
    private readonly LogFileMaintenanceService _maintenanceService;

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly LogCleanupOptions _options;

    /// <summary>
    /// 初始化启动磁盘门禁服务。
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="maintenanceService">日志维护服务。</param>
    /// <param name="options">日志清理配置。</param>
    public LogCleanupStartupGuardHostedService(
        ILogger<LogCleanupStartupGuardHostedService> logger,
        LogFileMaintenanceService maintenanceService,
        IOptions<LogCleanupOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await _maintenanceService.EnsureMinimumFreeSpaceForStartupAsync(
            AppContext.BaseDirectory,
            _options,
            cancellationToken);

        if (result.IsDisabled)
        {
            _logger.LogInformation(
                "启动磁盘门禁已关闭。ThresholdMb={ThresholdMb}",
                _options.StartupMinimumFreeSpaceMb);
            return;
        }

        if (result.IsSatisfied)
        {
            return;
        }

        throw new InvalidOperationException(
            $"启动磁盘门禁检查失败：{result.FailureReason ?? "未知原因"}");
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
