using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 同步执行服务实现。
/// </summary>
public class SyncExecutionService(
    IOracleSourceReader oracleSourceReader,
    ISyncStagingRepository stagingRepository,
    ISyncUpsertRepository upsertRepository,
    ISyncBatchRepository batchRepository,
    ISyncChangeLogRepository changeLogRepository,
    ISyncCheckpointRepository checkpointRepository,
    ILogger<SyncExecutionService> logger) : ISyncExecutionService
{
    /// <summary>失败检查点写入超时秒数。</summary>
    private const int ErrorCheckpointSaveTimeoutSeconds = 3;

    /// <summary>快照序列化配置。</summary>
    private static readonly JsonSerializerOptions SnapshotSerializerOptions = new()
    {
        WriteIndented = false,
    };

    /// <inheritdoc/>
    public async Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var readCount = 0;
        var insertCount = 0;
        var updateCount = 0;
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
            await batchRepository.MarkInProgressAsync(context.BatchId, DateTime.Now, ct);

            var pageNo = 1;
            while (!ct.IsCancellationRequested)
            {
                // 步骤1：按窗口分页读取源端增量数据。
                var readRequest = new SyncReadRequest
                {
                    TableCode = context.Definition.TableCode,
                    CursorColumn = context.Definition.CursorColumn,
                    PageNo = pageNo,
                    PageSize = context.Definition.PageSize,
                    Window = context.Window,
                    UniqueKeys = context.Definition.UniqueKeys,
                };
                var readResult = await oracleSourceReader.ReadIncrementalPageAsync(readRequest, ct);
                if (readResult.Rows.Count == 0)
                {
                    break;
                }

                // 步骤2：写入暂存并执行幂等合并。
                await stagingRepository.BulkInsertAsync(context.BatchId, pageNo, readResult.Rows, ct);
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

            // 步骤4：读取与合并成功后，先落变更日志。
            await changeLogRepository.WriteChangesAsync(pendingChanges, ct);

            // 步骤5：仅在读取、合并、日志写入全部完成后提交检查点。
            await checkpointRepository.SaveAsync(new SyncCheckpoint
            {
                TableCode = context.Definition.TableCode,
                LastBatchId = context.BatchId,
                LastSuccessCursorLocal = lastSuccessCursorLocal,
                LastSuccessTimeLocal = DateTime.Now,
                LastError = null,
            }, ct);

            var batchResult = new SyncBatchResult
            {
                BatchId = context.BatchId,
                TableCode = context.Definition.TableCode,
                WindowStartLocal = context.Window.WindowStartLocal,
                WindowEndLocal = context.Window.WindowEndLocal,
                ReadCount = readCount,
                InsertCount = insertCount,
                UpdateCount = updateCount,
                DeleteCount = 0,
                SkipCount = skipCount,
                Elapsed = stopwatch.Elapsed,
            };
            await batchRepository.CompleteBatchAsync(batchResult, DateTime.Now, ct);
            return batchResult;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogInformation("同步批次已取消。TableCode={TableCode}, BatchId={BatchId}", context.Definition.TableCode, context.BatchId);
            await batchRepository.FailBatchAsync(context.BatchId, "同步任务被取消。", DateTime.Now, CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "同步批次执行失败。TableCode={TableCode}, BatchId={BatchId}, Window=[{WindowStartLocal},{WindowEndLocal}], Checkpoint={Checkpoint}",
                context.Definition.TableCode,
                context.BatchId,
                context.Window.WindowStartLocal,
                context.Window.WindowEndLocal,
                context.Checkpoint.LastSuccessCursorLocal);

            using var errorCheckpointCts = new CancellationTokenSource(TimeSpan.FromSeconds(ErrorCheckpointSaveTimeoutSeconds));
            await batchRepository.FailBatchAsync(context.BatchId, ex.Message, DateTime.Now, CancellationToken.None);
            await checkpointRepository.SaveAsync(new SyncCheckpoint
            {
                TableCode = context.Definition.TableCode,
                LastBatchId = context.BatchId,
                LastSuccessCursorLocal = context.Checkpoint.LastSuccessCursorLocal,
                LastSuccessTimeLocal = context.Checkpoint.LastSuccessTimeLocal,
                LastError = ex.Message,
            }, errorCheckpointCts.Token);
            throw;
        }
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
        foreach (var row in rows)
        {
            var businessKey = BuildBusinessKey(context.Definition.UniqueKeys, row);
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
                ChangedTimeLocal = DateTime.Now,
            });
        }
    }

    /// <summary>
    /// 构建业务键文本。
    /// </summary>
    /// <param name="uniqueKeys">唯一键集合。</param>
    /// <param name="row">数据行。</param>
    /// <returns>业务键。</returns>
    private static string BuildBusinessKey(IReadOnlyList<string> uniqueKeys, IReadOnlyDictionary<string, object?> row)
    {
        if (uniqueKeys.Count == 0)
        {
            return string.Empty;
        }

        var keyValues = uniqueKeys.Select(key =>
            row.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty).ToArray();
        return JsonSerializer.Serialize(keyValues, SnapshotSerializerOptions);
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
}
