using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ApiWarmupHostedService(
    IApiWarmupService apiWarmupService,
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<ApiWarmupHostedService> logger) : IHostedService
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int WarmupTimeoutSeconds = 90;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly CancellationTokenSource _warmupCancellationTokenSource = new();
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private Task? _warmupTask;

    /// <summary>
    /// 启动接口预热后台服务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>启动任务。</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _warmupTask = Task.Run(async () =>
        {
            try
            {
                using var warmupTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(WarmupTimeoutSeconds));
                using var linkedWarmupCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    hostApplicationLifetime.ApplicationStopping,
                    _warmupCancellationTokenSource.Token,
                    warmupTimeoutCts.Token);
                await TryWarmupStepAsync("EF模型与分表上下文缓存", async () => await WarmupDbContextCacheAsync(linkedWarmupCts.Token), linkedWarmupCts.Token);
                await TryWarmupStepAsync("查询链路预热", async () => await apiWarmupService.WarmupAsync(linkedWarmupCts.Token), linkedWarmupCts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("启动预热执行已取消（超时 >{WarmupTimeoutSeconds}s 或宿主停止信号），已跳过剩余预热步骤。", WarmupTimeoutSeconds);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "启动预热执行失败，已降级跳过，不影响主机可用性。");
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止接口预热后台服务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>停止任务。</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _warmupCancellationTokenSource.Cancel();
        if (_warmupTask is not null)
        {
            await _warmupTask.WaitAsync(cancellationToken);
        }
    }

    private async Task WarmupDbContextCacheAsync(CancellationToken cancellationToken)
    {
        // 步骤：预热基础表上下文。
        await WarmupSingleDbContextAsync(string.Empty, cancellationToken);
        // 步骤：预热当月分表上下文。
        await WarmupSingleDbContextAsync(shardSuffixResolver.ResolveLocal(DateTime.Now), cancellationToken);
    }

    private async Task WarmupSingleDbContextAsync(string suffix, CancellationToken cancellationToken)
    {
        using var tableSuffixScope = TableSuffixScope.Use(suffix);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        _ = dbContext.Model;
        _ = await dbContext.BusinessTasks.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(cancellationToken);
        _ = await dbContext.ScanLogs.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(cancellationToken);
        _ = await dbContext.DropLogs.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(cancellationToken);
    }

    private async Task TryWarmupStepAsync(string stepName, Func<Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await action();
            logger.LogInformation("启动预热步骤完成：{StepName}", stepName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "启动预热步骤失败，已跳过：{StepName}", stepName);
        }
    }
}

