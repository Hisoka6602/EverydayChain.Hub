using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using EverydayChain.Hub.Domain.Sync;

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
            .Select((definition, index) => (definition, index))
            .ToList();
        var maxParallelTables = await configRepository.GetMaxParallelTablesAsync(ct);
        var results = new SyncBatchResult[orderedDefinitions.Count];
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = maxParallelTables,
        };

        await Parallel.ForEachAsync(orderedDefinitions, parallelOptions, async (item, token) =>
        {
            try
            {
                results[item.index] = await RunTableSyncAsync(item.definition.TableCode, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 全局取消时向外传播，停止整轮同步。
                throw;
            }
            catch (Exception ex)
            {
                // 单表失败不阻塞其余表继续推进，记录失败结果。
                logger.LogError(ex, "单表同步失败，其余表继续推进。TableCode={TableCode}", item.definition.TableCode);
                results[item.index] = new SyncBatchResult
                {
                    BatchId = string.Empty,
                    TableCode = item.definition.TableCode,
                    FailureRate = 1D,
                    FailureMessage = ex.Message,
                };
            }
        });

        return results.Where(x => x is not null).ToList().AsReadOnly();
    }
}
