using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using EverydayChain.Hub.Domain.Sync;
using Newtonsoft.Json;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义 SyncExecutionService 类型。
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
    IBusinessTaskStatusConsumeService businessTaskStatusConsumeService,
    ILogger<SyncExecutionService> logger) : ISyncExecutionService
{
    /// <summary>
    /// 存储 ErrorCheckpointSaveTimeoutSeconds 字段。
    /// </summary>
    private const int ErrorCheckpointSaveTimeoutSeconds = 3;
    /// <summary>
    /// 存储 StatusDrivenBusinessTaskLogicalTable 字段。
    /// </summary>
    private const string StatusDrivenBusinessTaskLogicalTable = "business_tasks";
    private const string FailBatchStatusUpdateErrorLogTemplate = "更新同步失败批次状态异常。TableCode={TableCode}, BatchId={BatchId}";
    private const string FailBatchOnCancelErrorLogTemplate = "在处理同步批次取消时更新批次失败。TableCode={TableCode}, BatchId={BatchId}";
    /// <summary>
    /// 存储 FullySkippedPageWarningInterval 字段。
    /// </summary>
    private const int FullySkippedPageWarningInterval = 100;
    private static readonly JsonSerializerSettings SnapshotSerializerSettings = new()
    {
        Formatting = Formatting.None,
    };

    public async Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct)
    {
        if (context.Definition.SyncMode == SyncMode.StatusDriven)
        {
            ValidateStatusDrivenBusinessTaskDefinition(context.Definition);
            return await ExecuteStatusDrivenBatchAsync(context, ct);
        }

        var stopwatch = Stopwatch.StartNew();
        var batchPersistedToRepository = false;
        var readCount = 0;
        var insertCount = 0;
        var updateCount = 0;
        var deleteCount = 0;
        var skipCount = 0;
        DateTime? lastSuccessCursorLocal = context.Checkpoint.LastSuccessCursorLocal;
        var pendingChanges = new List<SyncChangeLog>();
        var batchInitElapsedMs = 0L;
        var readElapsedMs = 0L;
        var stagingElapsedMs = 0L;
        var mergeElapsedMs = 0L;
        var deletionElapsedMs = 0L;
        var persistElapsedMs = 0L;
        var checkpointElapsedMs = 0L;
        try
        {
            var stepSw = Stopwatch.StartNew();
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
            batchInitElapsedMs = stepSw.ElapsedMilliseconds;

            var pageNo = 1;
            var processedPageCount = 0;
            while (!ct.IsCancellationRequested)
            {
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
                stepSw.Restart();
                var readResult = await oracleSourceReader.ReadIncrementalPageAsync(readRequest, ct);
                readElapsedMs += stepSw.ElapsedMilliseconds;
                if (readResult.Rows.Count == 0)
                {
                    if (pageNo == 1)
                    {
                        var uniqueKeysText = BuildUniqueKeysText(context.Definition.UniqueKeys);
                        logger.LogWarning(
                            "同步源端读取为空。TableCode={TableCode}, SourceSchema={SourceSchema}, SourceTable={SourceTable}, CursorColumn={CursorColumn}, WindowStartLocal={WindowStartLocal}, WindowEndLocal={WindowEndLocal}, UniqueKeys={UniqueKeys}",
                            context.Definition.TableCode,
                            context.Definition.SourceSchema,
                            context.Definition.SourceTable,
                            context.Definition.CursorColumn,
                            context.Window.WindowStartLocal,
                            context.Window.WindowEndLocal,
                            uniqueKeysText);
                    }
                    break;
                }

                stepSw.Restart();
                await stagingRepository.BulkInsertAsync(context.BatchId, pageNo, readResult.Rows, context.NormalizedExcludedColumns, ct);
                stagingElapsedMs += stepSw.ElapsedMilliseconds;
                SyncMergeResult mergeResult;
                Exception? mergeException = null;
                stepSw.Restart();
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

                    }
                }
                mergeElapsedMs += stepSw.ElapsedMilliseconds;

                readCount += readResult.Rows.Count;
                insertCount += mergeResult.InsertCount;
                updateCount += mergeResult.UpdateCount;
                skipCount += mergeResult.SkipCount;
                if (mergeResult.InsertCount == 0 && mergeResult.UpdateCount == 0 && mergeResult.SkipCount == readResult.Rows.Count)
                {
                    var shouldLogFullySkippedPageWarning = pageNo == 1 || pageNo % FullySkippedPageWarningInterval == 0;
                    if (shouldLogFullySkippedPageWarning)
                    {
                        var uniqueKeysText = BuildUniqueKeysText(context.Definition.UniqueKeys);
                        logger.LogWarning(
                            "同步读取到数据但目标端0写入（全部跳过，按页采样输出）。"
                            + "TableCode={TableCode}, BatchId={BatchId}, SourceSchema={SourceSchema}, SourceTable={SourceTable}, "
                            + "PageNo={PageNo}, ReadRows={ReadRows}, SkipRows={SkipRows}, UniqueKeys={UniqueKeys}, WarningInterval={WarningInterval}",
                            context.Definition.TableCode,
                            context.BatchId,
                            context.Definition.SourceSchema,
                            context.Definition.SourceTable,
                            pageNo,
                            readResult.Rows.Count,
                            mergeResult.SkipCount,
                            uniqueKeysText,
                            FullySkippedPageWarningInterval);
                    }
                }
                AppendChangeLogs(context, pendingChanges, readResult.Rows, mergeResult.ChangedOperations);
                if (mergeResult.LastSuccessCursorLocal.HasValue)
                {
                    lastSuccessCursorLocal = mergeResult.LastSuccessCursorLocal;
                }

                if (readResult.Rows.Count < context.Definition.PageSize)
                {
                    processedPageCount++;
                    break;
                }

                processedPageCount++;
                pageNo++;
            }

            stepSw.Restart();
            var deletionExecutionResult = await deletionExecutionService.ExecuteDeletionAsync(context, ct);
            deletionElapsedMs = stepSw.ElapsedMilliseconds;
            deleteCount = deletionExecutionResult.DeletedCount;
            foreach (var deletionChange in deletionExecutionResult.ChangeLogs)
            {
                pendingChanges.Add(deletionChange);
            }

            stepSw.Restart();
            await changeLogRepository.WriteChangesAsync(pendingChanges, ct);
            await deletionLogRepository.WriteDeletionsAsync(deletionExecutionResult.DeletionLogs, ct);
            persistElapsedMs = stepSw.ElapsedMilliseconds;

            stepSw.Restart();
            await checkpointRepository.SaveAsync(new SyncCheckpoint
            {
                TableCode = context.Definition.TableCode,
                LastBatchId = context.BatchId,
                LastSuccessCursorLocal = lastSuccessCursorLocal,
                LastSuccessTimeLocal = DateTime.Now,
                LastError = null,
            }, ct);
            checkpointElapsedMs = stepSw.ElapsedMilliseconds;

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
                FailureRate = 0.000M,
            };
            await batchRepository.CompleteBatchAsync(batchResult, DateTime.Now, ct);
            logger.LogInformation(
                "同步批次步骤耗时。TableCode={TableCode}, BatchId={BatchId}, PageCount={PageCount}, BatchInitMs={BatchInitMs}, ReadMs={ReadMs}, StagingMs={StagingMs}, MergeMs={MergeMs}, DeletionMs={DeletionMs}, PersistMs={PersistMs}, CheckpointMs={CheckpointMs}, TotalMs={TotalMs}",
                context.Definition.TableCode,
                context.BatchId,
                processedPageCount,
                batchInitElapsedMs,
                readElapsedMs,
                stagingElapsedMs,
                mergeElapsedMs,
                deletionElapsedMs,
                persistElapsedMs,
                checkpointElapsedMs,
                stopwatch.ElapsedMilliseconds);
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
                1.000M);
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
                1.000M);
            logger.LogError(ex, "同步批次执行失败。TableCode={TableCode}, BatchId={BatchId}, Window=[{WindowStartLocal},{WindowEndLocal}], Checkpoint={Checkpoint}",
                context.Definition.TableCode,
                context.BatchId,
                context.Window.WindowStartLocal,
                context.Window.WindowEndLocal,
                context.Checkpoint.LastSuccessCursorLocal);
            logger.LogError(
                "同步批次失败诊断信息。TableCode={TableCode}, BatchId={BatchId}, SourceSchema={SourceSchema}, SourceTable={SourceTable}, CursorColumn={CursorColumn}, UniqueKeys={UniqueKeys}",
                context.Definition.TableCode,
                context.BatchId,
                context.Definition.SourceSchema,
                context.Definition.SourceTable,
                context.Definition.CursorColumn,
                BuildUniqueKeysText(context.Definition.UniqueKeys));

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

    private static void ValidateStatusDrivenBusinessTaskDefinition(SyncTableDefinition definition)
    {
        if (!string.Equals(definition.TargetLogicalTable, StatusDrivenBusinessTaskLogicalTable, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"StatusDriven 配置无效：表 {definition.TableCode} 的 TargetLogicalTable 必须为 {StatusDrivenBusinessTaskLogicalTable}。");
        }

        if (definition.SourceType == BusinessTaskSourceType.Unknown)
        {
            throw new InvalidOperationException(
                $"StatusDriven 配置无效：表 {definition.TableCode} 的 SourceType 不能为 Unknown。");
        }

        if (string.IsNullOrWhiteSpace(definition.BusinessKeyColumn))
        {
            throw new InvalidOperationException(
                $"StatusDriven 配置无效：表 {definition.TableCode} 的 BusinessKeyColumn 不能为空白。");
        }
    }

    private static string BuildUniqueKeysText(IReadOnlyList<string> uniqueKeys)
    {
        return uniqueKeys.Count == 0 ? "未配置" : string.Join(",", uniqueKeys);
    }

    /// <summary>
    /// 计算同步批次日志与落库需要的三项核心指标。
    /// </summary>
    private static (decimal LagMinutes, decimal BacklogMinutes, decimal ThroughputRowsPerSecond) BuildMetrics(
        SyncWindow window,
        int processedRows,
        TimeSpan elapsed)
    {
        // 步骤：基于同步窗口边界计算滞后和积压分钟数。
        // 步骤：基于实际处理行数和耗时计算吞吐。
        return (
            CalculateLagMinutes(window.WindowEndLocal),
            CalculateBacklogMinutes(window.WindowStartLocal),
            CalculateThroughputRowsPerSecond(processedRows, elapsed));
    }

    /// <summary>
    /// 计算同步窗口结束时间到当前时间的滞后分钟数。
    /// </summary>
    /// <param name="windowEndLocal">同步窗口结束时间。</param>
    /// <returns>保留三位小数的滞后分钟数。</returns>
    private static decimal CalculateLagMinutes(DateTime windowEndLocal)
    {
        var lagTicks = DateTime.Now.Ticks - windowEndLocal.Ticks;
        return CalculateNonNegativeMinutes(lagTicks);
    }

    /// <summary>
    /// 计算同步窗口开始时间到当前时间的积压分钟数。
    /// </summary>
    /// <param name="windowStartLocal">同步窗口开始时间。</param>
    /// <returns>保留三位小数的积压分钟数。</returns>
    private static decimal CalculateBacklogMinutes(DateTime windowStartLocal)
    {
        var backlogTicks = DateTime.Now.Ticks - windowStartLocal.Ticks;
        return CalculateNonNegativeMinutes(backlogTicks);
    }

    /// <summary>
    /// 计算每秒处理行数。
    /// </summary>
    /// <param name="processedRows">已处理行数。</param>
    /// <param name="elapsed">实际耗时。</param>
    /// <returns>保留三位小数的每秒处理行数。</returns>
    private static decimal CalculateThroughputRowsPerSecond(int processedRows, TimeSpan elapsed)
    {
        if (processedRows <= 0 || elapsed.Ticks <= 0)
        {
            return 0.000M;
        }

        var throughput = processedRows * (decimal)TimeSpan.TicksPerSecond / elapsed.Ticks;
        return RoundFixedDecimal(throughput);
    }

    /// <summary>
    /// 将时钟差值转换为非负分钟数。
    /// </summary>
    /// <param name="ticks">时间差对应的 Tick 数。</param>
    /// <returns>保留三位小数的分钟数。</returns>
    private static decimal CalculateNonNegativeMinutes(long ticks)
    {
        if (ticks <= 0)
        {
            return 0.000M;
        }

        var minutes = ticks / (decimal)TimeSpan.TicksPerMinute;
        return RoundFixedDecimal(minutes);
    }

    /// <summary>
    /// 统一将小数规整为三位定点精度。
    /// </summary>
    /// <param name="value">待规整的小数值。</param>
    /// <returns>保留三位小数的定点值。</returns>
    private static decimal RoundFixedDecimal(decimal value)
    {
        return Math.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 执行 AppendChangeLogs 方法。
    /// </summary>
    private static void AppendChangeLogs(
        SyncExecutionContext context,
        ICollection<SyncChangeLog> changes,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyDictionary<string, SyncChangeOperationType> changedOperations)
    {
        // 步骤：执行 AppendChangeLogs 方法的核心处理流程。
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

    private static string BuildSnapshot(IReadOnlyDictionary<string, object?> row)
    {
        return JsonConvert.SerializeObject(row, SnapshotSerializerSettings);
    }

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

    private async Task<SyncBatchResult> ExecuteStatusDrivenBatchAsync(SyncExecutionContext context, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var batchPersistedToRepository = false;
        var batchInitElapsedMs = 0L;
        var consumeElapsedMs = 0L;
        var persistElapsedMs = 0L;
        var checkpointElapsedMs = 0L;
        try
        {
            var stepSw = Stopwatch.StartNew();
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
            batchInitElapsedMs = stepSw.ElapsedMilliseconds;

            stepSw.Restart();
            var consumeResult = await businessTaskStatusConsumeService.ConsumeAsync(
                context.Definition,
                context.BatchId,
                context.Window,
                ct);
            consumeElapsedMs = stepSw.ElapsedMilliseconds;

            stepSw.Restart();
            await changeLogRepository.WriteChangesAsync([], ct);
            await deletionLogRepository.WriteDeletionsAsync([], ct);
            persistElapsedMs = stepSw.ElapsedMilliseconds;

            stepSw.Restart();
            await checkpointRepository.SaveAsync(new SyncCheckpoint
            {
                TableCode = context.Definition.TableCode,
                LastBatchId = context.BatchId,
                LastSuccessCursorLocal = context.Checkpoint.LastSuccessCursorLocal,
                LastSuccessTimeLocal = DateTime.Now,
                LastError = null,
            }, ct);
            checkpointElapsedMs = stepSw.ElapsedMilliseconds;

            var metrics = BuildMetrics(context.Window, consumeResult.ReadCount, stopwatch.Elapsed);
            var batchResult = new SyncBatchResult
            {
                BatchId = context.BatchId,
                TableCode = context.Definition.TableCode,
                WindowStartLocal = context.Window.WindowStartLocal,
                WindowEndLocal = context.Window.WindowEndLocal,
                ReadCount = consumeResult.ReadCount,
                InsertCount = consumeResult.AppendCount,
                UpdateCount = 0,
                DeleteCount = 0,
                SkipCount = consumeResult.SkippedWriteBackCount,
                Elapsed = stopwatch.Elapsed,
                LagMinutes = metrics.LagMinutes,
                BacklogMinutes = metrics.BacklogMinutes,
                ThroughputRowsPerSecond = metrics.ThroughputRowsPerSecond,
                FailureRate = consumeResult.WriteBackFailCount > 0
                    ? RoundFixedDecimal(consumeResult.WriteBackFailCount / (decimal)Math.Max(1, consumeResult.ReadCount))
                    : 0.000M,
            };
            await batchRepository.CompleteBatchAsync(batchResult, DateTime.Now, ct);
            logger.LogInformation(
                "状态驱动批次步骤耗时。TableCode={TableCode}, BatchId={BatchId}, BatchInitMs={BatchInitMs}, ConsumeMs={ConsumeMs}, PersistMs={PersistMs}, CheckpointMs={CheckpointMs}, TotalMs={TotalMs}",
                context.Definition.TableCode,
                context.BatchId,
                batchInitElapsedMs,
                consumeElapsedMs,
                persistElapsedMs,
                checkpointElapsedMs,
                stopwatch.ElapsedMilliseconds);
            logger.LogInformation(
                "状态驱动消费同步指标。TableCode={TableCode}, BatchId={BatchId}, ReadCount={ReadCount}, AppendCount={AppendCount}, WriteBackCount={WriteBackCount}, WriteBackFailCount={WriteBackFailCount}, SkippedWriteBackCount={SkippedWriteBackCount}, PageCount={PageCount}, LagMinutes={LagMinutes}, ThroughputRowsPerSecond={ThroughputRowsPerSecond}",
                batchResult.TableCode,
                batchResult.BatchId,
                consumeResult.ReadCount,
                consumeResult.AppendCount,
                consumeResult.WriteBackCount,
                consumeResult.WriteBackFailCount,
                consumeResult.SkippedWriteBackCount,
                consumeResult.PageCount,
                batchResult.LagMinutes,
                batchResult.ThroughputRowsPerSecond);
            return batchResult;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var metrics = BuildMetrics(context.Window, 0, stopwatch.Elapsed);
            logger.LogWarning(
                "状态驱动消费同步指标。TableCode={TableCode}, BatchId={BatchId}, LagMinutes={LagMinutes}, ThroughputRowsPerSecond={ThroughputRowsPerSecond}, FailureRate={FailureRate}",
                context.Definition.TableCode,
                context.BatchId,
                metrics.LagMinutes,
                metrics.ThroughputRowsPerSecond,
                1.000M);
            logger.LogInformation("状态驱动消费批次已取消。TableCode={TableCode}, BatchId={BatchId}", context.Definition.TableCode, context.BatchId);
            await TryMarkBatchFailedAsync(batchPersistedToRepository, context, "同步任务被取消。", FailBatchOnCancelErrorLogTemplate);
            throw;
        }
        catch (Exception ex)
        {
            var metrics = BuildMetrics(context.Window, 0, stopwatch.Elapsed);
            logger.LogWarning(
                "状态驱动消费同步指标。TableCode={TableCode}, BatchId={BatchId}, LagMinutes={LagMinutes}, ThroughputRowsPerSecond={ThroughputRowsPerSecond}, FailureRate={FailureRate}",
                context.Definition.TableCode,
                context.BatchId,
                metrics.LagMinutes,
                metrics.ThroughputRowsPerSecond,
                1.000M);
            logger.LogError(ex, "状态驱动消费批次执行失败。TableCode={TableCode}, BatchId={BatchId}", context.Definition.TableCode, context.BatchId);
            using var errorCheckpointCts = new CancellationTokenSource(TimeSpan.FromSeconds(ErrorCheckpointSaveTimeoutSeconds));
            await TryMarkBatchFailedAsync(batchPersistedToRepository, context, ex.Message, FailBatchStatusUpdateErrorLogTemplate);
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
}

