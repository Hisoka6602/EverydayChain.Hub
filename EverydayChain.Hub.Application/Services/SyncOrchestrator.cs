using System.Diagnostics;
using EverydayChain.Hub.Application.Abstractions.Infrastructure;
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
    IDatabaseConnectivityService databaseConnectivityService,
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
            var connectivityFailureResult = await TryBuildConnectivityFailureResultAsync(definition.TableCode, ct);
            if (connectivityFailureResult is not null)
            {
                return connectivityFailureResult;
            }

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
            var leaseOwnerId = RuntimeLeaseOwnerId.Create();
            var acquiredTimeLocal = DateTime.Now;
            var leaseSeconds = Math.Clamp(_syncJobOptions.TableLeaseSeconds > 0 ? _syncJobOptions.TableLeaseSeconds : 900, 30, 86400);
            if (!await runtimeLeaseRepository.TryAcquireAsync(
                    leaseKey,
                    leaseOwnerId,
                    acquiredTimeLocal,
                    acquiredTimeLocal.AddSeconds(leaseSeconds),
                    ct))
            {
                var activeLease = await runtimeLeaseRepository.GetAsync(leaseKey, ct);
                if (activeLease is null)
                {
                    logger.LogWarning("Skipped table sync because another execution still owns the lease. TableCode={TableCode}", definition.TableCode);
                }
                else
                {
                    logger.LogWarning(
                        "Skipped table sync because another execution still owns the lease. TableCode={TableCode}, LeaseOwnerId={LeaseOwnerId}, LeaseAcquiredTimeLocal={LeaseAcquiredTimeLocal}, LeaseExpiresAtLocal={LeaseExpiresAtLocal}",
                        definition.TableCode,
                        activeLease.OwnerId,
                        activeLease.AcquiredTimeLocal,
                        activeLease.ExpiresAtLocal);
                }

                return new SyncBatchResult
                {
                    BatchId = string.Empty,
                    TableCode = definition.TableCode,
                    FailureRate = 1.000M,
                    FailureMessage = "表同步正在执行中。"
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync orchestration failed. TableCode={TableCode}", tableCode);
            return BuildSingleTableFailureResult(tableCode, ex);
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
        var connectivityFailureResults = await TryBuildConnectivityFailureResultsAsync(
            orderedDefinitions.Select(item => item.definition).ToList(),
            ct);
        if (connectivityFailureResults is not null)
        {
            return connectivityFailureResults;
        }

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
            if (maxParallelTables == 1)
            {
                logger.LogInformation(
                    "Current sync parallelism is explicitly configured as 1, so enabled tables will run serially. ConfiguredMaxParallelTables={ConfiguredMaxParallelTables}, EnabledTables={EnabledTables}",
                    maxParallelTables,
                    orderedDefinitions.Count);
            }
            else
            {
                logger.LogWarning(
                    "Current sync parallelism is 1, so enabled tables will run serially. ConfiguredMaxParallelTables={ConfiguredMaxParallelTables}, EnabledTables={EnabledTables}",
                    maxParallelTables,
                    orderedDefinitions.Count);
            }
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
                    FailureMessage = "单表同步失败，请查看详细日志。"
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

    private async Task<SyncBatchResult?> TryBuildConnectivityFailureResultAsync(string tableCode, CancellationToken ct)
    {
        var snapshot = await databaseConnectivityService.GetSnapshotAsync(ct);
        if (snapshot.IsAvailable)
        {
            return null;
        }

        var userMessage = snapshot.BuildUserMessage();
        logger.LogWarning(
            "Skipping table sync because required databases are unavailable. TableCode={TableCode}, Message={Message}",
            tableCode,
            userMessage);
        return BuildSingleTableFailureResult(tableCode, userMessage);
    }

    private async Task<IReadOnlyList<SyncBatchResult>?> TryBuildConnectivityFailureResultsAsync(
        IReadOnlyList<SyncTableDefinition> definitions,
        CancellationToken ct)
    {
        if (definitions.Count == 0)
        {
            return null;
        }

        var snapshot = await databaseConnectivityService.GetSnapshotAsync(ct);
        if (snapshot.IsAvailable)
        {
            return null;
        }

        var userMessage = snapshot.BuildUserMessage();
        logger.LogWarning(
            "Skipping all enabled table sync because required databases are unavailable. EnabledTables={EnabledTables}, Message={Message}",
            definitions.Count,
            userMessage);
        return definitions
            .Select(definition => BuildSingleTableFailureResult(definition.TableCode, userMessage))
            .ToList();
    }

    private static SyncBatchResult BuildSingleTableFailureResult(string tableCode, Exception exception)
    {
        return BuildSingleTableFailureResult(tableCode, exception.GetBaseException().Message);
    }

    private static SyncBatchResult BuildSingleTableFailureResult(string tableCode, string failureDetail)
    {
        return new SyncBatchResult
        {
            BatchId = string.Empty,
            TableCode = tableCode,
            FailureRate = 1.000M,
            FailureMessage = BuildFailureMessage(failureDetail)
        };
    }

    private static string BuildFailureMessage(string? failureDetail)
    {
        var baseMessage = failureDetail?.Trim() ?? string.Empty;
        var message = string.IsNullOrWhiteSpace(baseMessage)
            ? "单表同步失败，请查看详细日志。"
            : $"单表同步失败：{baseMessage}";

        return message.Length <= 512
            ? message
            : message[..512];
    }
}
