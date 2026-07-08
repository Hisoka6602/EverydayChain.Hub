using System.Diagnostics;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义 SyncOrchestrator 类型。
/// </summary>
public sealed class SyncOrchestrator(
    ISyncTaskConfigRepository configRepository,
    ISyncCheckpointRepository checkpointRepository,
    ISyncBatchRepository batchRepository,
    IRuntimeLeaseRepository runtimeLeaseRepository,
    ISyncWindowCalculator windowCalculator,
    ISyncExecutionService executionService,
    IOptions<SyncJobOptions> syncJobOptions,
    ILogger<SyncOrchestrator> logger) : ISyncOrchestrator
{
    /// <summary>
    /// 存储 DefaultParallelTablesSafetyCap 字段。
    /// </summary>
    private const int DefaultParallelTablesSafetyCap = 4;
    /// <summary>
    /// 存储 _syncJobOptions 字段。
    /// </summary>
    private readonly SyncJobOptions _syncJobOptions = syncJobOptions.Value;

    public async Task<SyncBatchResult> RunTableSyncAsync(string tableCode, CancellationToken ct)
    {
        try
        {
            var definition = await configRepository.GetByTableCodeAsync(tableCode, ct);
            var checkpoint = await checkpointRepository.GetAsync(tableCode, ct);
            var window = windowCalculator.CalculateWindow(definition, checkpoint, DateTime.Now);
            var parentBatchId = !string.IsNullOrWhiteSpace(checkpoint.LastError)
                ? await batchRepository.GetLatestFailedBatchIdAsync(tableCode, ct)
                : null;
            var context = new SyncExecutionContext
            {
                Definition = definition,
                Checkpoint = checkpoint,
                Window = window,
                BatchId = Guid.NewGuid().ToString("N"),
                ParentBatchId = parentBatchId,
                NormalizedExcludedColumns = SyncColumnFilter.NormalizeColumns(definition.ExcludedColumns),
            };

            var leaseKey = $"sync-table:{definition.TableCode.Trim()}";
            var leaseOwnerId = context.BatchId;
            var acquiredTimeLocal = DateTime.Now;
            var leaseSeconds = Math.Clamp(_syncJobOptions.TableLeaseSeconds > 0 ? _syncJobOptions.TableLeaseSeconds : 900, 30, 86400);
            if (!await runtimeLeaseRepository.TryAcquireAsync(
                    leaseKey,
                    leaseOwnerId,
                    acquiredTimeLocal,
                    acquiredTimeLocal.AddSeconds(leaseSeconds),
                    ct))
            {
                logger.LogWarning("Skipped table sync because another execution still owns the lease. TableCode={TableCode}", definition.TableCode);
                return new SyncBatchResult
                {
                    BatchId = string.Empty,
                    TableCode = definition.TableCode,
                    FailureRate = 1.000M,
                    FailureMessage = "Table sync is already running."
                };
            }

            try
            {
                return await executionService.ExecuteBatchAsync(context, ct);
            }
            finally
            {
                await runtimeLeaseRepository.ReleaseAsync(leaseKey, leaseOwnerId, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync orchestration failed. TableCode={TableCode}", tableCode);
            throw;
        }
    }

    public async Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct)
    {
        var definitions = await configRepository.ListEnabledAsync(ct);
        var orderedDefinitions = definitions
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.TableCode, StringComparer.OrdinalIgnoreCase)
            .Select((definition, index) => (definition, index))
            .ToList();
        var maxParallelTables = await configRepository.GetMaxParallelTablesAsync(ct);
        var effectiveParallelTables = ResolveEffectiveParallelTables(maxParallelTables, orderedDefinitions.Count);
        logger.LogInformation(
            "Starting multi-table sync. ConfiguredMaxParallelTables={ConfiguredMaxParallelTables}, EffectiveParallelTables={EffectiveParallelTables}, EnabledTables={EnabledTables}",
            maxParallelTables,
            effectiveParallelTables,
            orderedDefinitions.Count);
        if (maxParallelTables <= 0 && orderedDefinitions.Count > DefaultParallelTablesSafetyCap)
        {
            logger.LogWarning(
                "MaxParallelTables is invalid, so the safety cap is being used. ConfiguredMaxParallelTables={ConfiguredMaxParallelTables}, SafetyCap={SafetyCap}, EnabledTables={EnabledTables}",
                maxParallelTables,
                DefaultParallelTablesSafetyCap,
                orderedDefinitions.Count);
        }

        if (orderedDefinitions.Count > 1 && effectiveParallelTables == 1)
        {
            logger.LogWarning(
                "Current sync parallelism is 1, so enabled tables will run serially. ConfiguredMaxParallelTables={ConfiguredMaxParallelTables}, EnabledTables={EnabledTables}",
                maxParallelTables,
                orderedDefinitions.Count);
        }

        var results = new SyncBatchResult[orderedDefinitions.Count];
        using var concurrencyLimiter = new SemaphoreSlim(effectiveParallelTables, effectiveParallelTables);
        var runningCount = 0;
        var tableTasks = orderedDefinitions.Select(async item =>
        {
            await concurrencyLimiter.WaitAsync(ct);
            var currentRunning = Interlocked.Increment(ref runningCount);
            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation(
                "Table sync started. TableCode={TableCode}, RunningTables={RunningTables}, EffectiveParallelTables={EffectiveParallelTables}",
                item.definition.TableCode,
                currentRunning,
                effectiveParallelTables);

            try
            {
                results[item.index] = await RunTableSyncAsync(item.definition.TableCode, ct);
                logger.LogInformation(
                    "Table sync completed. TableCode={TableCode}, ElapsedMs={ElapsedMs}",
                    item.definition.TableCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                results[item.index] = new SyncBatchResult
                {
                    BatchId = string.Empty,
                    TableCode = item.definition.TableCode,
                    FailureRate = 1.000M,
                    FailureMessage = "Single-table sync failed. See detailed logs."
                };
            }
            finally
            {
                Interlocked.Decrement(ref runningCount);
                concurrencyLimiter.Release();
            }
        });
        await Task.WhenAll(tableTasks);
        return results;
    }

    private static int ResolveEffectiveParallelTables(int configuredMaxParallelTables, int enabledTableCount)
    {
        if (enabledTableCount <= 1)
        {
            return 1;
        }

        if (configuredMaxParallelTables <= 0)
        {
            return Math.Min(DefaultParallelTablesSafetyCap, enabledTableCount);
        }

        return Math.Min(configuredMaxParallelTables, enabledTableCount);
    }
}

