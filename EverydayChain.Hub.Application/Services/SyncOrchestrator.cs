using System.Diagnostics;
using Microsoft.Extensions.Logging;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Application.Abstractions.Persistence;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 同步编排服务实现。
/// </summary>
public class SyncOrchestrator(
    ISyncTaskConfigRepository configRepository,
    ISyncCheckpointRepository checkpointRepository,
    ISyncBatchRepository batchRepository,
    ISyncWindowCalculator windowCalculator,
    ISyncExecutionService executionService,
    ILogger<SyncOrchestrator> logger) : ISyncOrchestrator {

    /// <inheritdoc/>
    public async Task<SyncBatchResult> RunTableSyncAsync(string tableCode, CancellationToken ct) {
        try {
            var definition = await configRepository.GetByTableCodeAsync(tableCode, ct);
            var checkpoint = await checkpointRepository.GetAsync(tableCode, ct);
            var window = windowCalculator.CalculateWindow(definition, checkpoint, DateTime.Now);
            var parentBatchId = !string.IsNullOrWhiteSpace(checkpoint.LastError)
                ? await batchRepository.GetLatestFailedBatchIdAsync(tableCode, ct)
                : null;
            var context = new SyncExecutionContext {
                Definition = definition,
                Checkpoint = checkpoint,
                Window = window,
                BatchId = Guid.NewGuid().ToString("N"),
                ParentBatchId = parentBatchId,
                NormalizedExcludedColumns = SyncColumnFilter.NormalizeColumns(definition.ExcludedColumns),
            };
            return await executionService.ExecuteBatchAsync(context, ct);
        }
        catch (Exception ex) {
            logger.LogError(ex, "同步编排失败。TableCode={TableCode}", tableCode);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct) {
        var definitions = await configRepository.ListEnabledAsync(ct);
        var orderedDefinitions = definitions
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.TableCode, StringComparer.OrdinalIgnoreCase)
            .Select((definition, index) => (definition, index))
            .ToList();
        var maxParallelTables = await configRepository.GetMaxParallelTablesAsync(ct);
        var effectiveParallelTables = ResolveEffectiveParallelTables(maxParallelTables, orderedDefinitions.Count);
        logger.LogInformation(
            "开始执行多表同步调度。ConfiguredMaxParallelTables={ConfiguredMaxParallelTables}, EffectiveParallelTables={EffectiveParallelTables}, EnabledTables={EnabledTables}",
            maxParallelTables,
            effectiveParallelTables,
            orderedDefinitions.Count);
        if (orderedDefinitions.Count > 1 && effectiveParallelTables == 1) {
            logger.LogWarning(
                "当前同步并发度为 1，多表将串行执行。ConfiguredMaxParallelTables={ConfiguredMaxParallelTables}, EnabledTables={EnabledTables}",
                maxParallelTables,
                orderedDefinitions.Count);
        }

        var results = new SyncBatchResult[orderedDefinitions.Count];
        using var concurrencyLimiter = new SemaphoreSlim(effectiveParallelTables, effectiveParallelTables);
        var runningCount = 0;
        var tableTasks = orderedDefinitions.Select(async item => {
            await concurrencyLimiter.WaitAsync(ct);
            var currentRunning = Interlocked.Increment(ref runningCount);
            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation(
                "表同步已启动。TableCode={TableCode}, RunningTables={RunningTables}, EffectiveParallelTables={EffectiveParallelTables}",
                item.definition.TableCode,
                currentRunning,
                effectiveParallelTables);

            try {
                results[item.index] = await RunTableSyncAsync(item.definition.TableCode, ct);
                logger.LogInformation(
                    "表同步已完成。TableCode={TableCode}, ElapsedMs={ElapsedMs}",
                    item.definition.TableCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                // 全局取消时向外传播，停止整轮同步。
                throw;
            }
            catch (Exception) {
                // RunTableSyncAsync 内部已记录详细异常日志，此处仅构造失败结果，不重复记录。
                // 单表失败不阻塞其余表继续推进。
                results[item.index] = new SyncBatchResult {
                    BatchId = string.Empty,
                    TableCode = item.definition.TableCode,
                    FailureRate = 1D,
                    FailureMessage = "单表同步失败，详情见详细日志。",
                };
            }
            finally {
                Interlocked.Decrement(ref runningCount);
                concurrencyLimiter.Release();
            }
        });
        await Task.WhenAll(tableTasks);
        // 每个槽位在成功或异常分支中均已写入，Parallel.ForEachAsync 正常完成即保证所有槽位非空。
        return results;
    }

    /// <summary>
    /// 解析生效并行度：当配置小于等于 0 时，自动退化为“按启用表数并行”。
    /// </summary>
    /// <param name="configuredMaxParallelTables">配置并发上限。</param>
    /// <param name="enabledTableCount">启用表数量。</param>
    /// <returns>生效并发上限。</returns>
    private static int ResolveEffectiveParallelTables(int configuredMaxParallelTables, int enabledTableCount) {
        if (enabledTableCount <= 1) {
            return 1;
        }

        if (configuredMaxParallelTables <= 0) {
            return enabledTableCount;
        }

        return Math.Min(configuredMaxParallelTables, enabledTableCount);
    }
}
