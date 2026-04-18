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
                var projectionRow = TryBuildProjectionRow(definition, row);
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

            var projectionResult = projectionService.Project(new BusinessTaskProjectionRequest
            {
                Rows = projectionRows
            });
            foreach (var entity in projectionResult.Entities)
            {
                await businessTaskRepository.UpsertProjectionAsync(entity, ct);
                result.AppendCount++;
            }

            if (!profile.ShouldWriteBackRemoteStatus)
            {
                pageNo++;
                continue;
            }

            if (rowIds.Count == 0)
            {
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
    /// <param name="row">远端读取行。</param>
    /// <returns>投影行；关键字段缺失时返回空值。</returns>
    private BusinessTaskProjectionRow? TryBuildProjectionRow(SyncTableDefinition definition, IReadOnlyDictionary<string, object?> row)
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
        return new BusinessTaskProjectionRow
        {
            SourceTableCode = definition.TableCode,
            SourceType = definition.SourceType,
            BusinessKey = businessKey,
            Barcode = barcode,
            WaveCode = waveCode,
            WaveRemark = waveRemark,
            ProjectedTimeLocal = DateTime.Now
        };
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
