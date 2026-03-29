using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging;

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
    ILogger<SyncOrchestrator> logger) : ISyncOrchestrator
{
    /// <inheritdoc/>
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
            return await executionService.ExecuteBatchAsync(context, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "同步编排失败。TableCode={TableCode}", tableCode);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct)
    {
        var definitions = await configRepository.ListEnabledAsync(ct);
        var orderedDefinitions = definitions
            .OrderByDescending(x => x.Priority == SyncTablePriority.High)
            .ThenBy(x => x.TableCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var maxParallelTables = await configRepository.GetMaxParallelTablesAsync(ct);
        var results = new SyncBatchResult[orderedDefinitions.Count];
        using var semaphore = new SemaphoreSlim(maxParallelTables, maxParallelTables);
        var tasks = new List<Task>(orderedDefinitions.Count);
        for (var index = 0; index < orderedDefinitions.Count; index++)
        {
            ct.ThrowIfCancellationRequested();
            var currentIndex = index;
            var tableCode = orderedDefinitions[currentIndex].TableCode;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    results[currentIndex] = await RunTableSyncAsync(tableCode, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
        return results;
    }
}
