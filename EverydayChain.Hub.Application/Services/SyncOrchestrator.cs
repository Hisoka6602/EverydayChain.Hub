using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Persistence;
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
            .OrderByDescending(x => x.Priority)
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
            catch (Exception)
            {
                // RunTableSyncAsync 内部已记录详细异常日志，此处仅构造失败结果，不重复记录。
                // 单表失败不阻塞其余表继续推进。
                results[item.index] = new SyncBatchResult
                {
                    BatchId = string.Empty,
                    TableCode = item.definition.TableCode,
                    FailureRate = 1D,
                    FailureMessage = "单表同步失败，详情见详细日志。",
                };
            }
        });

        // 每个槽位在成功或异常分支中均已写入，Parallel.ForEachAsync 正常完成即保证所有槽位非空。
        return results;
    }
}
