using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 同步执行服务实现。
/// </summary>
public class SyncExecutionService(
    IOracleSourceReader oracleSourceReader,
    ISyncStagingRepository stagingRepository,
    ISyncUpsertRepository upsertRepository,
    IDeletionExecutionService deletionExecutionService,
    ISyncBatchRepository batchRepository,
    ISyncChangeLogRepository changeLogRepository,
    ISyncDeletionLogRepository deletionLogRepository,
    ISyncCheckpointRepository checkpointRepository,
    ILogger<SyncExecutionService> logger) : ISyncExecutionService
{
    /// <summary>失败检查点写入超时秒数。</summary>
    private const int ErrorCheckpointSaveTimeoutSeconds = 3;
    /// <summary>批次失败状态更新异常日志模板。</summary>
    private const string FailBatchStatusUpdateErrorLogTemplate = "更新同步失败批次状态异常。TableCode={TableCode}, BatchId={BatchId}";
    /// <summary>批次取消状态更新异常日志模板。</summary>
    private const string FailBatchOnCancelErrorLogTemplate = "在处理同步批次取消时更新批次失败。TableCode={TableCode}, BatchId={BatchId}";

    /// <summary>快照序列化配置。</summary>
    private static readonly JsonSerializerOptions SnapshotSerializerOptions = new()
    {
        WriteIndented = false,
    };

    /// <inheritdoc/>
    public async Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var batchPersistedToRepository = false;
        var readCount = 0;
        var insertCount = 0;
        var updateCount = 0;
        var deleteCount = 0;
        var skipCount = 0;
        DateTime? lastSuccessCursorLocal = context.Checkpoint.LastSuccessCursorLocal;
        var pendingChanges = new List<SyncChangeLog>();

        try
        {
            await batchRepository.CreateBatchAsync(new SyncBatch
            {
                BatchId = context.BatchId,
                ParentBatchId = context.ParentBatchId,
                TableCode = context.Definition.TableCode,
                WindowStartLocal = context.Window.WindowStartLocal,
                WindowEndLocal = context.Window.WindowEndLocal,
            }, ct);
            batchPersistedToRepository = true;
            await batchRepository.MarkInProgressAsync(context.BatchId, DateTime.Now, ct);

            var pageNo = 1;
            while (!ct.IsCancellationRequested)
            {
                // 步骤1：按窗口分页读取源端增量数据。
                var readRequest = new SyncReadRequest
                {
                    TableCode = context.Definition.TableCode,
                    SourceSchema = context.Definition.SourceSchema,
                    SourceTable = context.Definition.SourceTable,
                    CursorColumn = context.Definition.CursorColumn,
                    PageNo = pageNo,
                    PageSize = context.Definition.PageSize,
                    Window = context.Window,
                    UniqueKeys = context.Definition.UniqueKeys,
                    NormalizedExcludedColumns = context.NormalizedExcludedColumns,
                };
                var readResult = await oracleSourceReader.ReadIncrementalPageAsync(readRequest, ct);
                if (readResult.Rows.Count == 0)
                {
                    break;
                }

                // 步骤2：写入暂存并执行幂等合并。
                await stagingRepository.BulkInsertAsync(context.BatchId, pageNo, readResult.Rows, context.NormalizedExcludedColumns, ct);
                SyncMergeResult mergeResult;
                Exception? mergeException = null;
                try
                {
                    var stagingRows = await stagingRepository.GetPageRowsAsync(context.BatchId, pageNo, ct);
                    mergeResult = await upsertRepository.MergeFromStagingAsync(new SyncMergeRequest
                    {
                        TableCode = context.Definition.TableCode,
                        CursorColumn = context.Definition.CursorColumn,
                        UniqueKeys = context.Definition.UniqueKeys,
                        Rows = stagingRows,
                        NormalizedExcludedColumns = context.NormalizedExcludedColumns,
                    }, ct);
                }
                catch (Exception ex)
                {
                    mergeException = ex;
                    throw;
                }
                finally
                {
                    try
                    {
                        await stagingRepository.ClearPageAsync(context.BatchId, pageNo, ct);
                    }
                    catch (Exception clearEx)
                    {
                        logger.LogError(clearEx,
                            "清理同步暂存页失败。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}",
                            context.Definition.TableCode,
                            context.BatchId,
                            pageNo);
                        if (mergeException is null)
                        {
                            throw;
                        }

                        // 合并异常将向外抛出，此处不覆盖原始失败原因。
                    }
                }

                // 步骤3：累计统计并推进最大游标。
                readCount += readResult.Rows.Count;
                insertCount += mergeResult.InsertCount;
                updateCount += mergeResult.UpdateCount;
                skipCount += mergeResult.SkipCount;
                AppendChangeLogs(context, pendingChanges, readResult.Rows, mergeResult.ChangedOperations);
                if (mergeResult.LastSuccessCursorLocal.HasValue)
                {
                    lastSuccessCursorLocal = mergeResult.LastSuccessCursorLocal;
                }

                if (readResult.Rows.Count < context.Definition.PageSize)
                {
                    break;
                }

                pageNo++;
            }

            // 步骤4：执行删除同步（识别+执行+删除变更构建）。
            var deletionExecutionResult = await deletionExecutionService.ExecuteDeletionAsync(context, ct);
            deleteCount = deletionExecutionResult.DeletedCount;
            foreach (var deletionChange in deletionExecutionResult.ChangeLogs)
            {
                pendingChanges.Add(deletionChange);
            }

            // 步骤5：读取、合并、删除处理完成后，先落变更日志与删除日志。
            await changeLogRepository.WriteChangesAsync(pendingChanges, ct);
            await deletionLogRepository.WriteDeletionsAsync(deletionExecutionResult.DeletionLogs, ct);

            // 步骤6：仅在读取、合并、删除、日志写入全部完成后提交检查点。
            await checkpointRepository.SaveAsync(new SyncCheckpoint
            {
                TableCode = context.Definition.TableCode,
                LastBatchId = context.BatchId,
                LastSuccessCursorLocal = lastSuccessCursorLocal,
                LastSuccessTimeLocal = DateTime.Now,
                LastError = null,
            }, ct);

            var metrics = BuildMetrics(context.Window, readCount + deleteCount, stopwatch.Elapsed);
            var batchResult = new SyncBatchResult
            {
                BatchId = context.BatchId,
                TableCode = context.Definition.TableCode,
                WindowStartLocal = context.Window.WindowStartLocal,
                WindowEndLocal = context.Window.WindowEndLocal,
                ReadCount = readCount,
                InsertCount = insertCount,
                UpdateCount = updateCount,
                DeleteCount = deleteCount,
                SkipCount = skipCount,
                Elapsed = stopwatch.Elapsed,
                LagMinutes = metrics.LagMinutes,
                BacklogMinutes = metrics.BacklogMinutes,
                ThroughputRowsPerSecond = metrics.ThroughputRowsPerSecond,
                FailureRate = 0,
            };
            await batchRepository.CompleteBatchAsync(batchResult, DateTime.Now, ct);
            logger.LogInformation(
                "同步指标。TableCode={TableCode}, BatchId={BatchId}, LagMinutes={LagMinutes}, BacklogMinutes={BacklogMinutes}, ThroughputRowsPerSecond={ThroughputRowsPerSecond}, FailureRate={FailureRate}",
                batchResult.TableCode,
                batchResult.BatchId,
                batchResult.LagMinutes,
                batchResult.BacklogMinutes,
                batchResult.ThroughputRowsPerSecond,
                batchResult.FailureRate);
            return batchResult;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var metrics = BuildMetrics(context.Window, readCount + deleteCount, stopwatch.Elapsed);
            logger.LogWarning(
                "同步指标。TableCode={TableCode}, BatchId={BatchId}, LagMinutes={LagMinutes}, BacklogMinutes={BacklogMinutes}, ThroughputRowsPerSecond={ThroughputRowsPerSecond}, FailureRate={FailureRate}",
                context.Definition.TableCode,
                context.BatchId,
                metrics.LagMinutes,
                metrics.BacklogMinutes,
                metrics.ThroughputRowsPerSecond,
                1D);
            logger.LogInformation("同步批次已取消。TableCode={TableCode}, BatchId={BatchId}", context.Definition.TableCode, context.BatchId);
            await TryMarkBatchFailedAsync(
                batchPersistedToRepository,
                context,
                "同步任务被取消。",
                FailBatchOnCancelErrorLogTemplate);
            throw;
        }
        catch (Exception ex)
        {
            var metrics = BuildMetrics(context.Window, readCount + deleteCount, stopwatch.Elapsed);
            logger.LogWarning(
                "同步指标。TableCode={TableCode}, BatchId={BatchId}, LagMinutes={LagMinutes}, BacklogMinutes={BacklogMinutes}, ThroughputRowsPerSecond={ThroughputRowsPerSecond}, FailureRate={FailureRate}",
                context.Definition.TableCode,
                context.BatchId,
                metrics.LagMinutes,
                metrics.BacklogMinutes,
                metrics.ThroughputRowsPerSecond,
                1D);
            logger.LogError(ex, "同步批次执行失败。TableCode={TableCode}, BatchId={BatchId}, Window=[{WindowStartLocal},{WindowEndLocal}], Checkpoint={Checkpoint}",
                context.Definition.TableCode,
                context.BatchId,
                context.Window.WindowStartLocal,
                context.Window.WindowEndLocal,
                context.Checkpoint.LastSuccessCursorLocal);

            using var errorCheckpointCts = new CancellationTokenSource(TimeSpan.FromSeconds(ErrorCheckpointSaveTimeoutSeconds));
            await TryMarkBatchFailedAsync(
                batchPersistedToRepository,
                context,
                ex.Message,
                FailBatchStatusUpdateErrorLogTemplate);
            try
            {
                await checkpointRepository.SaveAsync(new SyncCheckpoint
                {
                    TableCode = context.Definition.TableCode,
                    LastBatchId = context.BatchId,
                    LastSuccessCursorLocal = context.Checkpoint.LastSuccessCursorLocal,
                    LastSuccessTimeLocal = context.Checkpoint.LastSuccessTimeLocal,
                    LastError = ex.Message,
                }, errorCheckpointCts.Token);
            }
            catch (Exception checkpointEx)
            {
                logger.LogError(checkpointEx, "写入失败检查点异常。TableCode={TableCode}, BatchId={BatchId}", context.Definition.TableCode, context.BatchId);
            }
            throw;
        }
    }

    /// <summary>
    /// 构建统一指标快照。
    /// </summary>
    /// <param name="window">同步窗口。</param>
    /// <param name="processedRows">处理行数。</param>
    /// <param name="elapsed">耗时。</param>
    /// <returns>指标元组。</returns>
    private static (double LagMinutes, double BacklogMinutes, double ThroughputRowsPerSecond) BuildMetrics(
        SyncWindow window,
        int processedRows,
        TimeSpan elapsed)
    {
        return (
            CalculateLagMinutes(window.WindowEndLocal),
            CalculateBacklogMinutes(window.WindowStartLocal),
            CalculateThroughputRowsPerSecond(processedRows, elapsed));
    }

    /// <summary>
    /// 计算窗口滞后分钟数。
    /// </summary>
    /// <param name="windowEndLocal">窗口结束时间。</param>
    /// <returns>滞后分钟数。</returns>
    private static double CalculateLagMinutes(DateTime windowEndLocal)
    {
        var lag = DateTime.Now - windowEndLocal;
        return lag.TotalMinutes < 0 ? 0 : lag.TotalMinutes;
    }

    /// <summary>
    /// 计算窗口积压分钟数。
    /// </summary>
    /// <param name="windowStartLocal">窗口起始时间。</param>
    /// <returns>积压分钟数。</returns>
    private static double CalculateBacklogMinutes(DateTime windowStartLocal)
    {
        var backlog = DateTime.Now - windowStartLocal;
        return backlog.TotalMinutes < 0 ? 0 : backlog.TotalMinutes;
    }

    /// <summary>
    /// 计算吞吐（每秒处理行数）。
    /// </summary>
    /// <param name="processedRows">处理行数。</param>
    /// <param name="elapsed">耗时。</param>
    /// <returns>吞吐。</returns>
    private static double CalculateThroughputRowsPerSecond(int processedRows, TimeSpan elapsed)
    {
        return elapsed.TotalSeconds <= 0 ? 0 : processedRows / elapsed.TotalSeconds;
    }

    /// <summary>
    /// 追加本页变更日志。
    /// </summary>
    /// <param name="context">执行上下文。</param>
    /// <param name="changes">待写入日志集合。</param>
    /// <param name="rows">当前页行数据。</param>
    /// <param name="changedOperations">业务键对应的变更操作类型映射（仅包含 Insert/Update 的变更键）。</param>
    private static void AppendChangeLogs(
        SyncExecutionContext context,
        ICollection<SyncChangeLog> changes,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyDictionary<string, SyncChangeOperationType> changedOperations)
    {
        var nowLocal = DateTime.Now;
        foreach (var row in rows)
        {
            var businessKey = SyncBusinessKeyBuilder.Build(row, context.Definition.UniqueKeys);
            if (string.IsNullOrWhiteSpace(businessKey))
            {
                continue;
            }

            if (!changedOperations.TryGetValue(businessKey, out var operationType))
            {
                continue;
            }

            changes.Add(new SyncChangeLog
            {
                BatchId = context.BatchId,
                ParentBatchId = context.ParentBatchId,
                TableCode = context.Definition.TableCode,
                OperationType = operationType,
                BusinessKey = businessKey,
                BeforeSnapshot = null,
                AfterSnapshot = BuildSnapshot(row),
                ChangedTimeLocal = nowLocal,
            });
        }
    }

    /// <summary>
    /// 构建行快照文本。
    /// </summary>
    /// <param name="row">数据行。</param>
    /// <returns>快照文本。</returns>
    private static string BuildSnapshot(IReadOnlyDictionary<string, object?> row)
    {
        return JsonSerializer.Serialize(row, SnapshotSerializerOptions);
    }

    /// <summary>
    /// 尝试标记批次失败（失败时仅记录日志，不影响主流程异常传递）。
    /// </summary>
    /// <param name="batchPersistedToRepository">是否已成功创建并持久化批次。</param>
    /// <param name="context">执行上下文。</param>
    /// <param name="errorMessage">错误信息。</param>
    /// <param name="onFailureLogTemplate">状态更新失败日志模板。</param>
    private async Task TryMarkBatchFailedAsync(bool batchPersistedToRepository, SyncExecutionContext context, string errorMessage, string onFailureLogTemplate)
    {
        if (!batchPersistedToRepository)
        {
            return;
        }

        try
        {
            await batchRepository.FailBatchAsync(context.BatchId, errorMessage, DateTime.Now, CancellationToken.None);
        }
        catch (Exception statusEx)
        {
            logger.LogError(statusEx, onFailureLogTemplate, context.Definition.TableCode, context.BatchId);
        }
    }
}
