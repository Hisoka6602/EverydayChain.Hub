using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Abstractions.Persistence;

namespace EverydayChain.Hub.Infrastructure.Sync.Services;

/// <summary>
/// 状态驱动消费服务实现。
/// </summary>
public class RemoteStatusConsumeService(
    IOracleStatusDrivenSourceReader sourceReader,
    ISqlServerAppendOnlyWriter appendOnlyWriter,
    IOracleRemoteStatusWriter remoteStatusWriter,
    IBusinessTaskMaterializer businessTaskMaterializer,
    IBusinessTaskRepository businessTaskRepository,
    ILogger<RemoteStatusConsumeService> logger) : IRemoteStatusConsumeService {

    /// <summary>状态驱动分页进度日志采样间隔。</summary>
    private const int PageProgressLogInterval = 20;

    /// <summary>慢步骤告警阈值（毫秒）。</summary>
    private const int SlowStepWarningThresholdMs = 3000;

    /// <inheritdoc/>
    public async Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, SyncWindow window, CancellationToken ct) {
        if (definition.SyncMode != SyncMode.StatusDriven) {
            throw new InvalidOperationException($"表 {definition.TableCode} 的同步模式不是 StatusDriven，禁止调用状态驱动消费链路。 ");
        }

        if (definition.StatusConsumeProfile is null) {
            throw new InvalidOperationException($"表 {definition.TableCode} 缺少 StatusConsumeProfile 配置。 ");
        }

        var profile = definition.StatusConsumeProfile;
        var normalizedExcludedColumns = SyncColumnFilter.NormalizeColumns(definition.ExcludedColumns);
        var hasCursorFilter = !string.IsNullOrWhiteSpace(definition.CursorColumn);
        if (hasCursorFilter) {
            var hasValidWindow = window.WindowStartLocal != default
                                 && window.WindowEndLocal != default
                                 && window.WindowStartLocal <= window.WindowEndLocal;
            if (!hasValidWindow) {
                logger.LogError(
                    "状态驱动消费游标时间窗口无效。TableCode={TableCode}, BatchId={BatchId}, CursorColumn={CursorColumn}, WindowStart={WindowStart}, WindowEnd={WindowEnd}, FailureReason={FailureReason}",
                    definition.TableCode,
                    batchId,
                    definition.CursorColumn,
                    window.WindowStartLocal,
                    window.WindowEndLocal,
                    "CursorColumn 非空时，WindowStart/WindowEnd 必须为有效本地时间且起始不得晚于结束。");
                throw new InvalidOperationException(
                    $"表 {definition.TableCode} 的状态驱动游标时间窗口无效：WindowStart={window.WindowStartLocal}, WindowEnd={window.WindowEndLocal}。");
            }

            logger.LogInformation(
                "状态驱动消费启用游标列时间范围过滤。TableCode={TableCode}, BatchId={BatchId}, CursorColumn={CursorColumn}, WindowStart={WindowStart}, WindowEnd={WindowEnd}",
                definition.TableCode,
                batchId,
                definition.CursorColumn,
                window.WindowStartLocal,
                window.WindowEndLocal);
        }

        var result = new RemoteStatusConsumeResult();
        var pageNo = 1;
        var shouldUseFixedFirstPage = profile.ShouldWriteBackRemoteStatus;

        while (!ct.IsCancellationRequested) {
            // 步骤1：按状态列分页读取待处理数据（支持 pending = null 的 IS NULL 语义）。
            // 当启用远端状态回写时，当前轮处理后待处理集合会收缩；固定读取第 1 页可避免 offset 翻页跳过未消费行。
            var currentPageNo = shouldUseFixedFirstPage ? 1 : pageNo;
            var pageStopwatch = Stopwatch.StartNew();
            var readStopwatch = Stopwatch.StartNew();
            var rows = await sourceReader.ReadPendingPageAsync(
                definition,
                profile,
                currentPageNo,
                profile.BatchSize,
                normalizedExcludedColumns,
                window,
                ct);
            if (currentPageNo == 1 || currentPageNo % PageProgressLogInterval == 0) {
                readStopwatch.Stop();
                if (currentPageNo == 1 || currentPageNo % PageProgressLogInterval == 0) {
                    logger.LogInformation(
                        "状态驱动分页读取完成。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, Rows={Rows}, ReadElapsedMs={ReadElapsedMs}, ShouldWriteBackRemoteStatus={ShouldWriteBackRemoteStatus}",
                        definition.TableCode,
                        batchId,
                        currentPageNo,
                        rows.Count,
                        readStopwatch.ElapsedMilliseconds,
                        profile.ShouldWriteBackRemoteStatus);
                }
                if (readStopwatch.ElapsedMilliseconds >= SlowStepWarningThresholdMs) {
                    logger.LogWarning(
                        "状态驱动分页读取耗时较高。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, ReadElapsedMs={ReadElapsedMs}, SlowThresholdMs={SlowThresholdMs}",
                        definition.TableCode,
                        batchId,
                        currentPageNo,
                        readStopwatch.ElapsedMilliseconds,
                        SlowStepWarningThresholdMs);
                }
            }
            if (rows.Count == 0) {
                logger.LogInformation(
                  "状态驱动读取到空页，结束本批次。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}",
                  definition.TableCode,
                  batchId,
                  currentPageNo);
                break;
            }

            result.PageCount++;
            result.ReadCount += rows.Count;

            // 步骤2：本地仅追加写入目标表（使用唯一键幂等去重，防止回写失败重试时产生重复键冲突）。
            var appendStopwatch = Stopwatch.StartNew();
            var appendCount = await appendOnlyWriter.AppendAsync(definition.TableCode, definition.TargetLogicalTable, rows, definition.UniqueKeys, ct);
            appendStopwatch.Stop();
            result.AppendCount += appendCount;
            await MaterializeBusinessTasksAsync(definition, rows, ct);
            if (appendStopwatch.ElapsedMilliseconds >= SlowStepWarningThresholdMs) {
                logger.LogWarning(
                    "状态驱动本地追加耗时较高。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, AppendRows={AppendRows}, AppendElapsedMs={AppendElapsedMs}, SlowThresholdMs={SlowThresholdMs}",
                    definition.TableCode,
                    batchId,
                    currentPageNo,
                    appendCount,
                    appendStopwatch.ElapsedMilliseconds,
                    SlowStepWarningThresholdMs);
            }
            if (!profile.ShouldWriteBackRemoteStatus) {
                pageStopwatch.Stop();
                logger.LogInformation(
                    "状态驱动分页处理完成。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, ReadRows={ReadRows}, AppendRows={AppendRows}, WriteBackRows={WriteBackRows}, PageElapsedMs={PageElapsedMs}",
                    definition.TableCode,
                    batchId,
                    currentPageNo,
                    rows.Count,
                    appendCount,
                    0,
                    pageStopwatch.ElapsedMilliseconds);
                pageNo++;
                continue;
            }

            // 步骤3：提取 __RowId，缺失行计入跳过回写统计。
            var rowIds = new List<string>(rows.Count);
            foreach (var row in rows) {
                if (row.TryGetValue("__RowId", out var rowIdObj) && rowIdObj is not null) {
                    var rowId = rowIdObj.ToString();
                    if (!string.IsNullOrWhiteSpace(rowId)) {
                        rowIds.Add(rowId);
                        continue;
                    }
                }

                result.SkippedWriteBackCount++;
            }

            if (rowIds.Count == 0) {
                pageNo++;
                continue;
            }

            // 步骤4：按 ROWID 回写远端状态；回写异常隔离为页级错误，不回滚已追加数据。
            var writeBackElapsedMs = 0L;
            var writeBackCount = 0;
            try {
                var writeBackStopwatch = Stopwatch.StartNew();
                writeBackCount = await remoteStatusWriter.WriteBackByRowIdAsync(definition, profile, batchId, rowIds, ct);
                writeBackStopwatch.Stop();
                writeBackElapsedMs = writeBackStopwatch.ElapsedMilliseconds;
                result.WriteBackCount += writeBackCount;
                if (writeBackElapsedMs >= SlowStepWarningThresholdMs) {
                    logger.LogWarning(
                        "状态驱动远端回写耗时较高。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, RowIdCount={RowIdCount}, WriteBackCount={WriteBackCount}, WriteBackElapsedMs={WriteBackElapsedMs}, SlowThresholdMs={SlowThresholdMs}",
                        definition.TableCode,
                        batchId,
                        currentPageNo,
                        rowIds.Count,
                        writeBackCount,
                        writeBackElapsedMs,
                        SlowStepWarningThresholdMs);
                }
                if (writeBackCount < rowIds.Count) {
                    result.WriteBackFailCount += rowIds.Count - writeBackCount;
                    logger.LogWarning(
                        "状态驱动远端回写存在未命中行。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, RowIdCount={RowIdCount}, WriteBackCount={WriteBackCount}, FailedRowIdCount={FailedRowIdCount}, FailureReason={FailureReason}",
                        definition.TableCode,
                        batchId,
                        currentPageNo,
                        rowIds.Count,
                        writeBackCount,
                        rowIds.Count - writeBackCount,
                        "数据库返回受影响行数小于请求回写行数，部分 ROWID 可能不存在或状态已变化。");
                }
            }
            catch (Exception ex) {
                result.WriteBackFailCount += rowIds.Count;
                logger.LogError(
                    ex,
                    "状态驱动远端回写失败，已隔离到页级。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, FailedRowIdCount={FailedRowIdCount}, FailureReason={FailureReason}",
                    definition.TableCode,
                    batchId,
                    currentPageNo,
                    rowIds.Count,
                    ex.Message);
                if (shouldUseFixedFirstPage) {
                    logger.LogWarning(
                        "状态驱动消费提前结束：固定第1页模式下远端回写失败，停止本批次以避免重复追加。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, FailedRowIdCount={FailedRowIdCount}",
                        definition.TableCode,
                        batchId,
                        currentPageNo,
                        rowIds.Count);
                    break;
                }
            }
            pageStopwatch.Stop();
            logger.LogInformation(
                "状态驱动分页处理完成。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, ReadRows={ReadRows}, AppendRows={AppendRows}, WriteBackRows={WriteBackRows}, WriteBackElapsedMs={WriteBackElapsedMs}, PageElapsedMs={PageElapsedMs}",
                definition.TableCode,
                batchId,
                currentPageNo,
                rows.Count,
                appendCount,
                writeBackCount,
                writeBackElapsedMs,
                pageStopwatch.ElapsedMilliseconds);
            if (!shouldUseFixedFirstPage) {
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

    /// <summary>
    /// 按同步行尝试物化业务任务（幂等）。
    /// </summary>
    /// <param name="definition">同步定义。</param>
    /// <param name="rows">当前页同步行。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task MaterializeBusinessTasksAsync(
        SyncTableDefinition definition,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct) {
        if (rows.Count == 0) {
            return;
        }

        foreach (var row in rows) {
            var request = BuildMaterializeRequest(definition, row);
            if (request is null) {
                continue;
            }

            var exists = await businessTaskRepository.FindByTaskCodeAsync(request.TaskCode, ct);
            if (exists is not null) {
                continue;
            }

            var entity = businessTaskMaterializer.Materialize(request);
            await businessTaskRepository.SaveAsync(entity, ct);
        }
    }

    /// <summary>
    /// 根据表定义与源行构建物化请求；不支持的表或关键字段缺失时返回空值。
    /// </summary>
    /// <param name="definition">同步定义。</param>
    /// <param name="row">源行。</param>
    /// <returns>物化请求。</returns>
    private static BusinessTaskMaterializeRequest? BuildMaterializeRequest(
        SyncTableDefinition definition,
        IReadOnlyDictionary<string, object?> row) {
        if (definition.TableCode.Equals("WmsSplitPickToLightCarton", StringComparison.OrdinalIgnoreCase)) {
            var barcode = ReadOptionalString(row, "CARTONNO");
            if (string.IsNullOrWhiteSpace(barcode)) {
                return null;
            }

            var taskCode = BuildTaskCode(definition.TableCode, barcode);
            return new BusinessTaskMaterializeRequest {
                TaskCode = taskCode,
                SourceTableCode = definition.TableCode,
                SourceType = BusinessTaskSourceType.Split,
                BusinessKey = barcode,
                Barcode = barcode,
                WaveCode = ReadOptionalString(row, "WAVENO"),
                WaveRemark = ReadOptionalString(row, "DESCR"),
                MaterializedTimeLocal = ReadOptionalDateTime(row, "ADDTIME") ?? DateTime.Now,
            };
        }

        if (definition.TableCode.Equals("WmsPickToWcs", StringComparison.OrdinalIgnoreCase)) {
            var barcode = ReadOptionalString(row, "SKUID");
            if (string.IsNullOrWhiteSpace(barcode)) {
                return null;
            }

            var taskCode = BuildTaskCode(definition.TableCode, barcode);
            return new BusinessTaskMaterializeRequest {
                TaskCode = taskCode,
                SourceTableCode = definition.TableCode,
                SourceType = BusinessTaskSourceType.FullCase,
                BusinessKey = barcode,
                Barcode = barcode,
                WaveCode = ReadOptionalString(row, "WAVENO"),
                WaveRemark = ReadOptionalString(row, "DESCR"),
                MaterializedTimeLocal = ReadOptionalDateTime(row, "ADDTIME") ?? DateTime.Now,
            };
        }

        return null;
    }

    /// <summary>
    /// 读取可选字符串列（忽略大小写键）。
    /// </summary>
    /// <param name="row">行数据。</param>
    /// <param name="columnName">列名。</param>
    /// <returns>列值字符串。</returns>
    private static string? ReadOptionalString(IReadOnlyDictionary<string, object?> row, string columnName) {
        if (!row.TryGetValue(columnName, out var value) || value is null) {
            return null;
        }

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>
    /// 读取可选时间列（忽略大小写键）。
    /// </summary>
    /// <param name="row">行数据。</param>
    /// <param name="columnName">列名。</param>
    /// <returns>本地时间。</returns>
    private static DateTime? ReadOptionalDateTime(IReadOnlyDictionary<string, object?> row, string columnName) {
        if (!row.TryGetValue(columnName, out var value) || value is null) {
            return null;
        }

        if (value is DateTime dateTime) {
            return dateTime;
        }

        return DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    /// <summary>
    /// 构建稳定任务编码，超长时附加哈希并截断。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="businessKey">业务键。</param>
    /// <returns>任务编码。</returns>
    private static string BuildTaskCode(string tableCode, string businessKey) {
        var raw = $"{tableCode}:{businessKey}";
        if (raw.Length <= 64) {
            return raw;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hash = Convert.ToHexString(bytes[..4]);
        var prefixMaxLength = 64 - hash.Length - 1;
        var prefix = raw[..prefixMaxLength];
        return $"{prefix}-{hash}";
    }
}
