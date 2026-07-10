using System.Globalization;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Sync.Services;

/// <summary>
/// 定义 BusinessTaskStatusConsumeService 类型。
/// </summary>
public class BusinessTaskStatusConsumeService(
    IOracleStatusDrivenSourceReader sourceReader,
    IOracleRemoteStatusWriter remoteStatusWriter,
    IBusinessTaskProjectionService projectionService,
    IBusinessTaskRepository businessTaskRepository,
    ILogger<BusinessTaskStatusConsumeService> logger) : IBusinessTaskStatusConsumeService
{
    public async Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, SyncWindow window, CancellationToken ct)
    {
        if (definition.StatusConsumeProfile is null)
        {
            throw new InvalidOperationException($"表 {definition.TableCode} 缺少 StatusConsumeProfile 配置。");
        }

        if (string.IsNullOrWhiteSpace(definition.BusinessKeyColumn))
        {
            throw new InvalidOperationException($"表 {definition.TableCode} 缺少 BusinessKeyColumn 配置。");
        }

        if (definition.SourceType == Domain.Enums.BusinessTaskSourceType.Unknown)
        {
            throw new InvalidOperationException($"表 {definition.TableCode} 的 SourceType 配置非法，不能为 Unknown。");
        }

        var profile = definition.StatusConsumeProfile;
        var normalizedExcludedColumns = SyncColumnFilter.NormalizeColumns(definition.ExcludedColumns);
        var result = new RemoteStatusConsumeResult();
        var pageNo = 1;
        var shouldUseFixedFirstPage = profile.ShouldWriteBackRemoteStatus;
        logger.LogInformation(
            "业务任务状态驱动消费开始。TableCode={TableCode}, BatchId={BatchId}, FixedFirstPageMode={FixedFirstPageMode}, StatusBatchSize={StatusBatchSize}, IgnorePendingStatusValue={IgnorePendingStatusValue}, ShouldWriteBackRemoteStatus={ShouldWriteBackRemoteStatus}, WindowStartLocal={WindowStartLocal}, WindowEndLocal={WindowEndLocal}",
            definition.TableCode,
            batchId,
            shouldUseFixedFirstPage,
            profile.BatchSize,
            profile.IgnorePendingStatusValue,
            profile.ShouldWriteBackRemoteStatus,
            window.WindowStartLocal,
            window.WindowEndLocal);
        while (!ct.IsCancellationRequested)
        {
            var currentPageNo = shouldUseFixedFirstPage ? 1 : pageNo;
            var rows = await sourceReader.ReadPendingPageAsync(
                definition,
                profile,
                currentPageNo,
                profile.BatchSize,
                normalizedExcludedColumns,
                window,
                ct);
            if (rows.Count == 0)
            {
                logger.LogInformation(
                    "业务任务状态驱动消费页读取为空。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, FixedFirstPageMode={FixedFirstPageMode}, ReadCountSoFar={ReadCountSoFar}, AppendCountSoFar={AppendCountSoFar}, WriteBackCountSoFar={WriteBackCountSoFar}",
                    definition.TableCode,
                    batchId,
                    currentPageNo,
                    shouldUseFixedFirstPage,
                    result.ReadCount,
                    result.AppendCount,
                    result.WriteBackCount);
                break;
            }

            result.PageCount++;
            result.ReadCount += rows.Count;
            var projectionRows = new List<BusinessTaskProjectionRow>(rows.Count);
            var rowIds = new List<string>(rows.Count);
            foreach (var row in rows)
            {
                var projectionRow = TryBuildProjectionRow(definition, batchId, row);
                if (projectionRow is null)
                {
                    continue;
                }

                projectionRows.Add(projectionRow);
                if (profile.ShouldWriteBackRemoteStatus)
                {
                    if (TryReadNonEmptyString(row, "__RowId", out var rowId))
                    {
                        rowIds.Add(rowId);
                    }
                    else
                    {
                        result.SkippedWriteBackCount++;
                    }
                }
            }

            if (projectionRows.Count == 0 && shouldUseFixedFirstPage)
            {
                logger.LogWarning(
                    "业务任务状态驱动消费提前结束：固定第1页模式下当前页无可投影行，避免死循环。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, RowCount={RowCount}",
                    definition.TableCode,
                    batchId,
                    currentPageNo,
                    rows.Count);
                break;
            }

            var projectionResult = projectionService.Project(new BusinessTaskProjectionRequest
            {
                Rows = projectionRows
            });
            result.AppendCount += await businessTaskRepository.UpsertProjectionBatchAsync(projectionResult.Entities, ct);
            AdvanceLastSuccessCursor(result, projectionRows);

            if (!profile.ShouldWriteBackRemoteStatus)
            {
                pageNo++;
                continue;
            }

            if (rowIds.Count == 0)
            {
                if (shouldUseFixedFirstPage)
                {
                    logger.LogWarning(
                        "业务任务状态驱动消费提前结束：固定第1页模式下当前页无可回写 ROWID，避免死循环。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, RowCount={RowCount}",
                        definition.TableCode,
                        batchId,
                        currentPageNo,
                        rows.Count);
                    break;
                }

                if (!shouldUseFixedFirstPage)
                {
                    pageNo++;
                }

                continue;
            }

            try
            {
                var writeBackCount = await remoteStatusWriter.WriteBackByRowIdAsync(definition, profile, batchId, rowIds, ct);
                result.WriteBackCount += writeBackCount;
                if (writeBackCount < rowIds.Count)
                {
                    result.WriteBackFailCount += rowIds.Count - writeBackCount;
                }
            }
            catch (Exception ex)
            {
                result.WriteBackFailCount += rowIds.Count;
                logger.LogError(
                    ex,
                    "业务任务状态驱动远端回写失败。TableCode={TableCode}, BatchId={BatchId}, PageNo={PageNo}, FailedRowIdCount={FailedRowIdCount}, FailureReason={FailureReason}",
                    definition.TableCode,
                    batchId,
                    currentPageNo,
                    rowIds.Count,
                    ex.Message);
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

        return result;
    }

    private static void AdvanceLastSuccessCursor(RemoteStatusConsumeResult result, IReadOnlyList<BusinessTaskProjectionRow> rows)
    {
        foreach (var row in rows)
        {
            if (!result.LastSuccessCursorLocal.HasValue || row.ProjectedTimeLocal > result.LastSuccessCursorLocal.Value)
            {
                result.LastSuccessCursorLocal = row.ProjectedTimeLocal;
            }
        }
    }

    /// <summary>
    /// 执行 TryBuildProjectionRow 方法。
    /// </summary>
    private BusinessTaskProjectionRow? TryBuildProjectionRow(
        SyncTableDefinition definition,
        string batchId,
        IReadOnlyDictionary<string, object?> row)
    {
        // 步骤：执行 TryBuildProjectionRow 方法的核心处理流程。
        if (!TryReadNonEmptyString(row, definition.BusinessKeyColumn, out var businessKey))
        {
            logger.LogWarning(
                "业务任务状态驱动投影跳过：缺少业务键。TableCode={TableCode}, BusinessKeyColumn={BusinessKeyColumn}",
                definition.TableCode,
                definition.BusinessKeyColumn);
            return null;
        }

        TryReadOptionalString(row, definition.BarcodeColumn, out var barcode);
        TryReadOptionalString(row, definition.WaveCodeColumn, out var waveCode);
        TryReadOptionalString(row, definition.WaveRemarkColumn, out var waveRemark);
        TryReadOptionalString(row, definition.WorkingAreaColumn, out var workingArea);
        TryReadOptionalString(row, definition.OrderIdColumn, out var orderId);
        TryReadOptionalString(row, definition.StoreIdColumn, out var storeId);
        TryReadOptionalString(row, definition.StoreNameColumn, out var storeName);
        TryReadOptionalString(row, definition.ProductCodeColumn, out var productCode);
        TryReadOptionalString(row, definition.PickLocationColumn, out var pickLocation);
        var projectedTimeLocal = ResolveProjectedTimeLocal(definition, batchId, businessKey, row);
        return new BusinessTaskProjectionRow
        {
            SourceTableCode = definition.TableCode,
            SourceType = definition.SourceType,
            BusinessKey = businessKey,
            Barcode = barcode,
            WaveCode = waveCode,
            WaveRemark = waveRemark,
            WorkingArea = workingArea,
            OrderId = orderId,
            StoreId = storeId,
            StoreName = storeName,
            ProductCode = productCode,
            PickLocation = pickLocation,
            ProjectedTimeLocal = projectedTimeLocal
        };
    }

    /// <summary>
    /// 执行 ResolveProjectedTimeLocal 方法。
    /// </summary>
    private DateTime ResolveProjectedTimeLocal(
        SyncTableDefinition definition,
        string batchId,
        string businessKey,
        IReadOnlyDictionary<string, object?> row)
    {
        // 步骤：执行 ResolveProjectedTimeLocal 方法的核心处理流程。
        if (TryResolveProjectedTimeFromCursorColumn(definition, row, out var projectedTimeLocal, out var fallbackReason))
        {
            return projectedTimeLocal;
        }

        logger.LogError(
            "远端业务时间缺失，无法确定分表月份，禁止继续写入业务任务。TableCode={TableCode}, BatchId={BatchId}, CursorColumn={CursorColumn}, BusinessKey={BusinessKey}, FailureReason={FailureReason}",
            definition.TableCode,
            batchId,
            definition.CursorColumn,
            businessKey,
            fallbackReason);
        throw new InvalidOperationException(
            $"远端业务时间缺失，无法确定分表月份，禁止继续写入业务任务。TableCode={definition.TableCode}, BatchId={batchId}, CursorColumn={definition.CursorColumn}, BusinessKey={businessKey}, FailureReason={fallbackReason}");
    }

    /// <summary>
    /// 执行 TryResolveProjectedTimeFromCursorColumn 方法。
    /// </summary>
    private static bool TryResolveProjectedTimeFromCursorColumn(
        SyncTableDefinition definition,
        IReadOnlyDictionary<string, object?> row,
        out DateTime projectedTimeLocal,
        out string failureReason)
    {
        // 步骤：执行 TryResolveProjectedTimeFromCursorColumn 方法的核心处理流程。
        projectedTimeLocal = default;
        failureReason = string.Empty;
        if (string.IsNullOrWhiteSpace(definition.CursorColumn))
        {
            failureReason = "未配置 CursorColumn";
            return false;
        }

        if (!row.TryGetValue(definition.CursorColumn, out var rawValue) || rawValue is null)
        {
            failureReason = "游标列值缺失";
            return false;
        }

        try
        {
            if (TryConvertToLocalDateTime(rawValue, out projectedTimeLocal, out failureReason))
            {
                return true;
            }

            failureReason = $"游标列值不可解析，RawType={rawValue.GetType().Name}";
            return false;
        }
        catch (Exception ex)
        {
            failureReason = $"游标列值解析异常：{ex.Message}";
            return false;
        }
    }

    private static bool TryConvertToLocalDateTime(object rawValue, out DateTime localTime, out string failureReason)
    {
        localTime = default;
        failureReason = string.Empty;
        switch (rawValue)
        {
            case DateTime dateTime:
                if (TryNormalizeLocalDateTime(dateTime, out localTime, out failureReason))
                {
                    return true;
                }

                return false;
            case DateTimeOffset:
                failureReason = "不支持包含时区偏移的时间值";
                return false;
            case string text when !string.IsNullOrWhiteSpace(text):
                if (DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedLocal)
                    || DateTime.TryParse(text.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsedLocal))
                {
                    if (TryNormalizeLocalDateTime(parsedLocal, out localTime, out failureReason))
                    {
                        return true;
                    }

                    return false;
                }

                failureReason = "字符串时间格式不可解析";
                break;
            default:
                if (rawValue is IConvertible)
                {
                    var convertedTime = Convert.ToDateTime(rawValue, CultureInfo.InvariantCulture);
                    if (TryNormalizeLocalDateTime(convertedTime, out localTime, out failureReason))
                    {
                        return true;
                    }

                    return false;
                }

                failureReason = "值类型不支持时间转换";
                break;
        }

        return false;
    }

    private static bool TryNormalizeLocalDateTime(DateTime time, out DateTime localTime, out string failureReason)
    {
        if (time.Kind == DateTimeKind.Local)
        {
            localTime = time;
            failureReason = string.Empty;
            return true;
        }

        if (time.Kind != DateTimeKind.Unspecified)
        {
            localTime = default;
            failureReason = $"不支持的时间语义 Kind={time.Kind}";
            return false;
        }

        localTime = DateTime.SpecifyKind(time, DateTimeKind.Local);
        failureReason = string.Empty;
        return true;
    }

    private static bool TryReadNonEmptyString(IReadOnlyDictionary<string, object?> row, string columnName, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return false;
        }

        if (!row.TryGetValue(columnName, out var raw) || raw is null)
        {
            return false;
        }

        var text = raw.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text;
        return true;
    }

    private static bool TryReadOptionalString(IReadOnlyDictionary<string, object?> row, string? columnName, out string? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return false;
        }

        if (!row.TryGetValue(columnName, out var raw) || raw is null)
        {
            return false;
        }

        var text = raw.ToString()?.Trim();
        value = string.IsNullOrWhiteSpace(text) ? null : text;
        return true;
    }
}

