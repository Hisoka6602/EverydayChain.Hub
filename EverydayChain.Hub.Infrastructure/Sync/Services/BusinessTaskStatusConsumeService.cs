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
/// 业务任务状态驱动消费服务实现。
/// </summary>
public class BusinessTaskStatusConsumeService(
    IOracleStatusDrivenSourceReader sourceReader,
    IOracleRemoteStatusWriter remoteStatusWriter,
    IBusinessTaskProjectionService projectionService,
    IBusinessTaskRepository businessTaskRepository,
    ILogger<BusinessTaskStatusConsumeService> logger) : IBusinessTaskStatusConsumeService
{
    /// <summary>
    /// 执行一轮状态驱动消费。
    /// </summary>
    /// <param name="definition">同步定义。</param>
    /// <param name="batchId">批次号。</param>
    /// <param name="window">时间窗口。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>消费结果。</returns>
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
                break;
            }

            result.PageCount++;
            result.ReadCount += rows.Count;
            var projectionRows = new List<BusinessTaskProjectionRow>(rows.Count);
            var rowIds = new List<string>(rows.Count);
            foreach (var row in rows)
            {
                var projectionRow = TryBuildProjectionRow(definition, batchId, window, row);
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

    /// <summary>
    /// 构建单行投影模型。
    /// </summary>
    /// <param name="definition">同步定义。</param>
    /// <param name="batchId">批次号。</param>
    /// <param name="window">同步窗口。</param>
    /// <param name="row">远端读取行。</param>
    /// <returns>投影行；关键字段缺失时返回空值。</returns>
    private BusinessTaskProjectionRow? TryBuildProjectionRow(
        SyncTableDefinition definition,
        string batchId,
        SyncWindow window,
        IReadOnlyDictionary<string, object?> row)
    {
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
        var projectedTimeLocal = ResolveProjectedTimeLocal(definition, batchId, window, row);
        return new BusinessTaskProjectionRow
        {
            SourceTableCode = definition.TableCode,
            SourceType = definition.SourceType,
            BusinessKey = businessKey,
            Barcode = barcode,
            WaveCode = waveCode,
            WaveRemark = waveRemark,
            ProjectedTimeLocal = projectedTimeLocal
        };
    }

    /// <summary>
    /// 解析投影业务时间，优先使用源业务时间，其次窗口结束时间，最后才降级当前本地时间。
    /// </summary>
    /// <param name="definition">同步定义。</param>
    /// <param name="batchId">批次号。</param>
    /// <param name="window">同步窗口。</param>
    /// <param name="row">远端读取行。</param>
    /// <returns>已解析的投影业务时间（本地时间）。</returns>
    private DateTime ResolveProjectedTimeLocal(
        SyncTableDefinition definition,
        string batchId,
        SyncWindow window,
        IReadOnlyDictionary<string, object?> row)
    {
        if (TryResolveProjectedTimeLocal(definition, batchId, window, row, out var projectedTimeLocal))
        {
            return projectedTimeLocal;
        }

        logger.LogWarning(
            "业务任务状态驱动投影时间缺失，已降级使用当前本地时间。TableCode={TableCode}, BatchId={BatchId}, CursorColumn={CursorColumn}",
            definition.TableCode,
            batchId,
            definition.CursorColumn);
        return DateTime.Now;
    }

    /// <summary>
    /// 尝试解析投影业务时间。
    /// </summary>
    /// <param name="definition">同步定义。</param>
    /// <param name="batchId">批次号。</param>
    /// <param name="window">同步窗口。</param>
    /// <param name="row">远端读取行。</param>
    /// <param name="projectedTimeLocal">解析结果。</param>
    /// <returns>解析成功返回 true，否则返回 false。</returns>
    private bool TryResolveProjectedTimeLocal(
        SyncTableDefinition definition,
        string batchId,
        SyncWindow window,
        IReadOnlyDictionary<string, object?> row,
        out DateTime projectedTimeLocal)
    {
        if (TryResolveProjectedTimeFromCursorColumn(definition, batchId, row, out projectedTimeLocal))
        {
            return true;
        }

        if (window.WindowEndLocal != default)
        {
            projectedTimeLocal = NormalizeLocalDateTime(window.WindowEndLocal);
            return true;
        }

        projectedTimeLocal = default;
        return false;
    }

    /// <summary>
    /// 从游标列尝试解析投影业务时间。
    /// </summary>
    /// <param name="definition">同步定义。</param>
    /// <param name="batchId">批次号。</param>
    /// <param name="row">远端读取行。</param>
    /// <param name="projectedTimeLocal">解析结果。</param>
    /// <returns>解析成功返回 true，否则返回 false。</returns>
    private bool TryResolveProjectedTimeFromCursorColumn(
        SyncTableDefinition definition,
        string batchId,
        IReadOnlyDictionary<string, object?> row,
        out DateTime projectedTimeLocal)
    {
        projectedTimeLocal = default;
        if (string.IsNullOrWhiteSpace(definition.CursorColumn))
        {
            return false;
        }

        if (!row.TryGetValue(definition.CursorColumn, out var rawValue) || rawValue is null)
        {
            return false;
        }

        try
        {
            if (TryConvertToLocalDateTime(rawValue, out projectedTimeLocal))
            {
                return true;
            }

            logger.LogWarning(
                "业务任务状态驱动投影时间解析失败：游标列值不可解析。TableCode={TableCode}, BatchId={BatchId}, CursorColumn={CursorColumn}, RawValue={RawValue}, RawType={RawType}",
                definition.TableCode,
                batchId,
                definition.CursorColumn,
                rawValue,
                rawValue.GetType().FullName);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "业务任务状态驱动投影时间解析异常。TableCode={TableCode}, BatchId={BatchId}, CursorColumn={CursorColumn}, RawType={RawType}",
                definition.TableCode,
                batchId,
                definition.CursorColumn,
                rawValue.GetType().FullName);
            return false;
        }
    }

    /// <summary>
    /// 尝试将源值转换为本地时间。
    /// </summary>
    /// <param name="rawValue">源值。</param>
    /// <param name="localTime">转换后的本地时间。</param>
    /// <returns>转换成功返回 true，否则返回 false。</returns>
    private static bool TryConvertToLocalDateTime(object rawValue, out DateTime localTime)
    {
        switch (rawValue)
        {
            case DateTime dateTime:
                localTime = NormalizeLocalDateTime(dateTime);
                return true;
            case DateTimeOffset dateTimeOffset:
                localTime = dateTimeOffset.LocalDateTime;
                return true;
            case string text when !string.IsNullOrWhiteSpace(text):
                if (DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedLocal)
                    || DateTime.TryParse(text.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsedLocal))
                {
                    localTime = NormalizeLocalDateTime(parsedLocal);
                    return true;
                }

                break;
            default:
                if (rawValue is IConvertible)
                {
                    var convertedTime = Convert.ToDateTime(rawValue, CultureInfo.InvariantCulture);
                    localTime = NormalizeLocalDateTime(convertedTime);
                    return true;
                }

                break;
        }

        localTime = default;
        return false;
    }

    /// <summary>
    /// 规范化时间 Kind，确保后续按本地时间语义处理。
    /// </summary>
    /// <param name="time">原始时间。</param>
    /// <returns>规范化后的本地时间。</returns>
    private static DateTime NormalizeLocalDateTime(DateTime time)
    {
        if (time.Kind == DateTimeKind.Local)
        {
            return time;
        }

        return DateTime.SpecifyKind(time, DateTimeKind.Local);
    }

    /// <summary>
    /// 读取必填文本列。
    /// </summary>
    /// <param name="row">行数据。</param>
    /// <param name="columnName">列名。</param>
    /// <param name="value">读取结果。</param>
    /// <returns>读取成功返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 读取可选文本列。
    /// </summary>
    /// <param name="row">行数据。</param>
    /// <param name="columnName">列名。</param>
    /// <param name="value">读取结果。</param>
    /// <returns>读取成功返回 true，否则返回 false。</returns>
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
