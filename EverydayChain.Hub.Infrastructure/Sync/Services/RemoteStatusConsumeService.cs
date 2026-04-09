using EverydayChain.Hub.Application.Sync.Abstractions;
using EverydayChain.Hub.Application.Sync.Models;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Infrastructure.Sync.Abstractions;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Sync.Services;

/// <summary>
/// 状态驱动消费服务实现。
/// </summary>
public class RemoteStatusConsumeService(
    IOracleStatusDrivenSourceReader sourceReader,
    ISqlServerAppendOnlyWriter appendOnlyWriter,
    IOracleRemoteStatusWriter remoteStatusWriter,
    ILogger<RemoteStatusConsumeService> logger) : IRemoteStatusConsumeService
{
    /// <inheritdoc/>
    public async Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, CancellationToken ct)
    {
        if (definition.SyncMode != SyncMode.StatusDriven)
        {
            throw new InvalidOperationException($"表 {definition.TableCode} 的同步模式不是 StatusDriven，禁止调用状态驱动消费链路。 ");
        }

        if (definition.StatusConsumeProfile is null)
        {
            throw new InvalidOperationException($"表 {definition.TableCode} 缺少 StatusConsumeProfile 配置。 ");
        }

        var profile = definition.StatusConsumeProfile;
        var normalizedExcludedColumns = SyncColumnFilter.NormalizeColumns(definition.ExcludedColumns);
        var result = new RemoteStatusConsumeResult();
        var pageNo = 1;
        var shouldUseFixedFirstPage = profile.ShouldWriteBackRemoteStatus;

        while (!ct.IsCancellationRequested)
        {
            // 步骤1：按状态列分页读取待处理数据（支持 pending = null 的 IS NULL 语义）。
            // 当启用远端状态回写时，当前轮处理后待处理集合会收缩；固定读取第 1 页可避免 offset 翻页跳过未消费行。
            var currentPageNo = shouldUseFixedFirstPage ? 1 : pageNo;
            var rows = await sourceReader.ReadPendingPageAsync(
                definition,
                profile,
                currentPageNo,
                profile.BatchSize,
                normalizedExcludedColumns,
                ct);
            if (rows.Count == 0)
            {
                break;
            }

            result.PageCount++;
            result.ReadCount += rows.Count;

            // 步骤2：本地仅追加写入目标表（禁止 merge/delete）。
            var appendCount = await appendOnlyWriter.AppendAsync(definition.TableCode, definition.TargetLogicalTable, rows, ct);
            result.AppendCount += appendCount;

            if (!profile.ShouldWriteBackRemoteStatus)
            {
                pageNo++;
                continue;
            }

            // 步骤3：提取 __RowId，缺失行计入跳过回写统计。
            var rowIds = new List<string>(rows.Count);
            foreach (var row in rows)
            {
                if (row.TryGetValue("__RowId", out var rowIdObj) && rowIdObj is not null)
                {
                    var rowId = rowIdObj.ToString();
                    if (!string.IsNullOrWhiteSpace(rowId))
                    {
                        rowIds.Add(rowId);
                        continue;
                    }
                }

                result.SkippedWriteBackCount++;
            }

            if (rowIds.Count == 0)
            {
                pageNo++;
                continue;
            }

            // 步骤4：按 ROWID 回写远端状态；回写异常隔离为页级错误，不回滚已追加数据。
            try
            {
                var writeBackCount = await remoteStatusWriter.WriteBackByRowIdAsync(definition, profile, rowIds, ct);
                result.WriteBackCount += writeBackCount;
                if (writeBackCount < rowIds.Count)
                {
                    result.WriteBackFailCount += rowIds.Count - writeBackCount;
                    logger.LogWarning(
                        "状态驱动远端回写存在未命中行。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, RowIdCount={RowIdCount}, WriteBackCount={WriteBackCount}",
                        definition.TableCode,
                        batchId,
                        currentPageNo,
                        rowIds.Count,
                        writeBackCount);
                }
            }
            catch (Exception ex)
            {
                result.WriteBackFailCount += rowIds.Count;
                logger.LogError(
                    ex,
                    "状态驱动远端回写失败，已隔离到页级。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, FailedRowIdCount={FailedRowIdCount}",
                    definition.TableCode,
                    batchId,
                    currentPageNo,
                    rowIds.Count);
                if (shouldUseFixedFirstPage)
                {
                    break;
                }
            }

            if (!shouldUseFixedFirstPage)
            {
                pageNo++;
            }
        }

        logger.LogInformation(
            "状态驱动消费完成。TableCode={TableCode}, BatchId={BatchId}, ReadCount={ReadCount}, AppendCount={AppendCount}, WriteBackCount={WriteBackCount}, WriteBackFailCount={WriteBackFailCount}, SkippedWriteBackCount={SkippedWriteBackCount}, PageCount={PageCount}",
            definition.TableCode,
            batchId,
            result.ReadCount,
            result.AppendCount,
            result.WriteBackCount,
            result.WriteBackFailCount,
            result.SkippedWriteBackCount,
            result.PageCount);
        return result;
    }
}
