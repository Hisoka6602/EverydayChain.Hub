using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Startup;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 启动后执行一次性预热，并按配置持续保温查询链路。
/// </summary>
public sealed class ApiWarmupHostedService(
    IApiWarmupService apiWarmupService,
    IApiEndpointWarmupService apiEndpointWarmupService,
    IDashboardSnapshotService dashboardSnapshotService,
    IDatabaseConnectivityService databaseConnectivityService,
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    QueryCacheOptions queryCacheOptions,
    IApiWarmupState apiWarmupState,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<ApiWarmupHostedService> logger) : IHostedService
{
    /// <summary>
    /// 存储 WarmupTimeoutSeconds 字段。
    /// </summary>
    private const int WarmupTimeoutSeconds = 300;

    /// <summary>
    /// 存储 _warmupCancellationTokenSource 字段。
    /// </summary>
    private readonly CancellationTokenSource _warmupCancellationTokenSource = new();
    /// <summary>
    /// 存储 _bootstrapWarmupTask 字段。
    /// </summary>
    private Task? _bootstrapWarmupTask;
    /// <summary>
    /// 存储 _maintenanceWarmupTask 字段。
    /// </summary>
    private Task? _maintenanceWarmupTask;

    /// <summary>
    /// 执行 StartAsync 方法。
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 步骤：异步启动一次性预热流程，避免阻塞宿主启动线程。
        apiWarmupState.MarkStarted("Bootstrap", "启动预热任务已创建，等待本地数据库可用。");

        _bootstrapWarmupTask = Task.Run(
            async () => await RunBootstrapWarmupAsync(cancellationToken),
            CancellationToken.None);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 执行 StopAsync 方法。
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // 步骤：统一取消启动预热与持续保温任务，并等待后台任务收敛退出。
        _warmupCancellationTokenSource.Cancel();

        var tasks = new[] { _bootstrapWarmupTask, _maintenanceWarmupTask }
            .Where(task => task is not null)
            .Cast<Task>()
            .ToArray();
        if (tasks.Length == 0)
        {
            return;
        }

        await Task.WhenAll(tasks.Select(task => task.WaitAsync(cancellationToken)));
    }

    /// <summary>
    /// 执行 RunBootstrapWarmupAsync 方法。
    /// </summary>
    private async Task RunBootstrapWarmupAsync(CancellationToken cancellationToken)
    {
        // 步骤：先完成宿主启动期的一次性预热，再在预热成功后按配置启动持续保温循环。
        try
        {
            using var warmupTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(WarmupTimeoutSeconds));
            using var linkedWarmupCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                hostApplicationLifetime.ApplicationStopping,
                _warmupCancellationTokenSource.Token,
                warmupTimeoutCts.Token);
            var warmupCancellationToken = linkedWarmupCts.Token;

            var localSqlServerState = await databaseConnectivityService.GetLocalSqlServerStateAsync(warmupCancellationToken);
            if (!localSqlServerState.IsAvailable)
            {
                apiWarmupState.MarkSkipped("LocalSqlUnavailable", localSqlServerState.Description);
                logger.LogWarning("启动预热已跳过：{Message}", localSqlServerState.Description);
                return;
            }

            apiWarmupState.MarkProgress("DbContextWarmup", "正在预热 EF 模型与分表上下文缓存。");
            await TryWarmupStepAsync(
                "EF 模型与分表上下文缓存",
                async () => await WarmupDbContextCacheAsync(warmupCancellationToken),
                warmupCancellationToken);

            apiWarmupState.MarkProgress("DashboardSnapshotWarmup", "正在刷新看板快照，确保后续查询优先命中快照路径。");
            await TryWarmupStepAsync(
                "看板快照刷新",
                async () => await dashboardSnapshotService.RefreshAsync(warmupCancellationToken),
                warmupCancellationToken);

            apiWarmupState.MarkProgress("QueryServiceWarmup", "正在预热查询服务链路。");
            await TryWarmupStepAsync(
                "查询链路预热",
                async () => await apiWarmupService.WarmupAsync(warmupCancellationToken),
                warmupCancellationToken);

            apiWarmupState.MarkProgress("WaitForApplicationStarted", "查询服务预热完成，等待 Web 宿主开始监听。");
            await WaitForApplicationStartedAsync(warmupCancellationToken);

            apiWarmupState.MarkProgress("HttpEndpointWarmup", "Web 宿主已开始监听，正在预热 HTTP 查询端点。");
            await TryWarmupStepAsync(
                "HTTP 端点预热",
                async () => await apiEndpointWarmupService.WarmupAsync(warmupCancellationToken),
                warmupCancellationToken);

            apiWarmupState.MarkCompleted("Completed", "启动预热已完成，查询服务与 HTTP 端点均已预热。");
            TryStartBackgroundWarmupLoop(cancellationToken);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested
            || hostApplicationLifetime.ApplicationStopping.IsCancellationRequested
            || _warmupCancellationTokenSource.IsCancellationRequested)
        {
            apiWarmupState.MarkFailed("Cancelled", $"启动预热已取消或超时（>{WarmupTimeoutSeconds} 秒）。");
            logger.LogWarning(
                "启动预热执行已取消（超时 >{WarmupTimeoutSeconds}s 或宿主停止信号），已跳过剩余预热步骤。",
                WarmupTimeoutSeconds);
        }
        catch (Exception ex)
        {
            apiWarmupState.MarkFailed("Failed", $"启动预热失败：{ex.Message}");
            logger.LogWarning(ex, "启动预热执行失败，已降级跳过，不影响宿主可用性。");
        }
    }

    /// <summary>
    /// 执行 TryStartBackgroundWarmupLoop 方法。
    /// </summary>
    private void TryStartBackgroundWarmupLoop(CancellationToken cancellationToken)
    {
        // 步骤：根据查询缓存配置决定是否启用持续保温，并确保同一宿主只创建一条保温循环。
        if (!queryCacheOptions.Enabled || !queryCacheOptions.BackgroundWarmupEnabled)
        {
            logger.LogInformation("查询链路保温任务已禁用。");
            return;
        }

        if (_maintenanceWarmupTask is not null)
        {
            return;
        }

        logger.LogInformation(
            "查询链路保温任务已启用。IntervalSeconds={IntervalSeconds}",
            queryCacheOptions.BackgroundWarmupIntervalSeconds);

        _maintenanceWarmupTask = Task.Run(
            async () => await RunBackgroundWarmupLoopAsync(cancellationToken),
            CancellationToken.None);
    }

    /// <summary>
    /// 执行 RunBackgroundWarmupLoopAsync 方法。
    /// </summary>
    private async Task RunBackgroundWarmupLoopAsync(CancellationToken cancellationToken)
    {
        // 步骤：按固定间隔续热查询链路，避免启动预热产物在真实流量到达前就过期失效。
        try
        {
            using var linkedWarmupCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                hostApplicationLifetime.ApplicationStopping,
                _warmupCancellationTokenSource.Token);
            var warmupCancellationToken = linkedWarmupCts.Token;
            var interval = TimeSpan.FromSeconds(queryCacheOptions.BackgroundWarmupIntervalSeconds);

            while (!warmupCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(interval, warmupCancellationToken);

                var localSqlServerState = await databaseConnectivityService.GetLocalSqlServerStateAsync(warmupCancellationToken);
                if (!localSqlServerState.IsAvailable)
                {
                    logger.LogWarning("查询链路保温跳过：{Message}", localSqlServerState.Description);
                    continue;
                }

                await TryWarmupStepAsync(
                    "查询链路保温",
                    async () => await apiWarmupService.WarmupAsync(warmupCancellationToken),
                    warmupCancellationToken);
            }
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested
            || hostApplicationLifetime.ApplicationStopping.IsCancellationRequested
            || _warmupCancellationTokenSource.IsCancellationRequested)
        {
            logger.LogInformation("查询链路保温任务已停止。");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "查询链路保温任务执行失败，已终止本轮保温循环。");
        }
    }

    /// <summary>
    /// 执行 WarmupDbContextCacheAsync 方法。
    /// </summary>
    private async Task WarmupDbContextCacheAsync(CancellationToken cancellationToken)
    {
        await WarmupSingleDbContextAsync(string.Empty, cancellationToken);
        await WarmupSingleDbContextAsync(shardSuffixResolver.ResolveLocal(DateTime.Now), cancellationToken);
    }

    /// <summary>
    /// 执行 WarmupSingleDbContextAsync 方法。
    /// </summary>
    private async Task WarmupSingleDbContextAsync(string suffix, CancellationToken cancellationToken)
    {
        // 步骤：创建指定分表后缀的上下文并访问关键实体，尽量把 EF 模型与分表解析缓存预先拉热。
        using var tableSuffixScope = TableSuffixScope.Use(suffix);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        _ = dbContext.Model;
        _ = await dbContext.BusinessTasks
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(1)
            .ToListAsync(cancellationToken);
        _ = await dbContext.ScanLogs
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(1)
            .ToListAsync(cancellationToken);
        _ = await dbContext.DropLogs
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(1)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 执行 TryWarmupStepAsync 方法。
    /// </summary>
    private async Task TryWarmupStepAsync(string stepName, Func<Task> action, CancellationToken cancellationToken)
    {
        // 步骤：执行单个预热步骤并兜底记录异常，保证局部失败不会阻断其余预热任务。
        try
        {
            await action();
            logger.LogInformation("启动预热步骤完成。StepName={StepName}", stepName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "启动预热步骤失败，已跳过。StepName={StepName}", stepName);
        }
    }

    /// <summary>
    /// 执行 WaitForApplicationStartedAsync 方法。
    /// </summary>
    private async Task WaitForApplicationStartedAsync(CancellationToken cancellationToken)
    {
        if (hostApplicationLifetime.ApplicationStarted.IsCancellationRequested)
        {
            return;
        }

        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var startedRegistration = hostApplicationLifetime.ApplicationStarted.Register(static state =>
        {
            ((TaskCompletionSource)state!).TrySetResult();
        }, completionSource);
        using var cancelledRegistration = cancellationToken.Register(static state =>
        {
            ((TaskCompletionSource)state!).TrySetCanceled();
        }, completionSource);

        await completionSource.Task;
    }
}
