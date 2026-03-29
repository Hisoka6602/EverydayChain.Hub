using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 同步执行服务实现。
/// </summary>
public class SyncExecutionService(
    IOracleSourceReader oracleSourceReader,
    ISyncStagingRepository stagingRepository,
    ISyncUpsertRepository upsertRepository,
    ISyncCheckpointRepository checkpointRepository,
    ILogger<SyncExecutionService> logger) : ISyncExecutionService
{
    /// <inheritdoc/>
    public async Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct)
    {
        var startedAt = DateTime.Now;
        var readCount = 0;
        var insertCount = 0;
        var updateCount = 0;
        var skipCount = 0;
        DateTime? lastSuccessCursorLocal = context.Checkpoint.LastSuccessCursorLocal;

        try
        {
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
                var stagingRows = await stagingRepository.GetPageRowsAsync(context.BatchId, pageNo, ct);
                var mergeResult = await upsertRepository.MergeFromStagingAsync(new SyncMergeRequest
                {
                    TableCode = context.Definition.TableCode,
                    CursorColumn = context.Definition.CursorColumn,
                    UniqueKeys = context.Definition.UniqueKeys,
                    Rows = stagingRows,
                }, ct);
                await stagingRepository.ClearPageAsync(context.BatchId, pageNo, ct);

                // 步骤3：累计统计并推进最大游标。
                readCount += readResult.Rows.Count;
                insertCount += mergeResult.InsertCount;
                updateCount += mergeResult.UpdateCount;
                skipCount += mergeResult.SkipCount;
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

            // 步骤4：仅在读取与合并完成后提交检查点。
            await checkpointRepository.SaveAsync(new SyncCheckpoint
            {
                TableCode = context.Definition.TableCode,
                LastBatchId = context.BatchId,
                LastSuccessCursorLocal = lastSuccessCursorLocal,
                LastSuccessTimeLocal = DateTime.Now,
                LastError = null,
            }, ct);

            return new SyncBatchResult
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
                Elapsed = DateTime.Now - startedAt,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "同步批次执行失败。TableCode={TableCode}, BatchId={BatchId}, Window=[{WindowStartLocal},{WindowEndLocal}], Checkpoint={Checkpoint}",
                context.Definition.TableCode,
                context.BatchId,
                context.Window.WindowStartLocal,
                context.Window.WindowEndLocal,
                context.Checkpoint.LastSuccessCursorLocal);

            await checkpointRepository.SaveAsync(new SyncCheckpoint
            {
                TableCode = context.Definition.TableCode,
                LastBatchId = context.Checkpoint.LastBatchId,
                LastSuccessCursorLocal = context.Checkpoint.LastSuccessCursorLocal,
                LastSuccessTimeLocal = context.Checkpoint.LastSuccessTimeLocal,
                LastError = ex.Message,
            }, ct);
            throw;
        }
    }
}
