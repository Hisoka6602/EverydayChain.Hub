using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskProjectionBackfillService(
    ISyncTaskConfigRepository syncTaskConfigRepository,
    IBusinessTaskRepository businessTaskRepository,
    IOracleSourceReader oracleSourceReader,
    IBusinessTaskProjectionService businessTaskProjectionService,
    ILogger<BusinessTaskProjectionBackfillService> logger) : IBusinessTaskProjectionBackfillService
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int MaxAllowedCount = 10000;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int MaxAllowedBatchSize = 500;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public async Task<BusinessTaskProjectionBackfillPreviewResult> PreviewAsync(
        BusinessTaskProjectionBackfillPreviewCommand command,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (command is null)
        {
            return BusinessTaskProjectionBackfillPreviewResult.Fail("Preview command cannot be null.");
        }

        if (command.EndTimeLocal <= command.StartTimeLocal)
        {
            return BusinessTaskProjectionBackfillPreviewResult.Fail("EndTimeLocal must be greater than StartTimeLocal.");
        }

        IReadOnlyList<SyncTableDefinition> definitions;
        try
        {
            definitions = await LoadDefinitionsAsync(command.TableCode, cancellationToken);
        }
        catch (InvalidOperationException exception) when (!string.IsNullOrWhiteSpace(command.TableCode))
        {
            return BusinessTaskProjectionBackfillPreviewResult.Fail(exception.Message);
        }

        var eligibleDefinitions = definitions
            .Where(IsEligibleDefinition)
            .OrderBy(definition => definition.TableCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (eligibleDefinitions.Count == 0)
        {
            return BusinessTaskProjectionBackfillPreviewResult.Fail("No eligible projection-backed table configuration was found.");
        }

        var tableResults = new List<BusinessTaskProjectionBackfillPreviewTableResult>(eligibleDefinitions.Count);
        foreach (var definition in eligibleDefinitions)
        {
            var gapSummary = await businessTaskRepository.CountProjectionBackfillGapsAsync(
                definition.TableCode,
                command.StartTimeLocal,
                command.EndTimeLocal,
                !string.IsNullOrWhiteSpace(definition.OrderIdColumn),
                !string.IsNullOrWhiteSpace(definition.StoreIdColumn),
                !string.IsNullOrWhiteSpace(definition.StoreNameColumn),
                !string.IsNullOrWhiteSpace(definition.ProductCodeColumn),
                !string.IsNullOrWhiteSpace(definition.PickLocationColumn),
                cancellationToken);
            tableResults.Add(new BusinessTaskProjectionBackfillPreviewTableResult
            {
                TableCode = definition.TableCode,
                CandidateCount = gapSummary.CandidateCount,
                MissingOrderIdCount = gapSummary.MissingOrderIdCount,
                MissingStoreIdCount = gapSummary.MissingStoreIdCount,
                MissingStoreNameCount = gapSummary.MissingStoreNameCount,
                MissingProductCodeCount = gapSummary.MissingProductCodeCount,
                MissingPickLocationCount = gapSummary.MissingPickLocationCount
            });
        }

        var result = new BusinessTaskProjectionBackfillPreviewResult
        {
            IsSuccess = true,
            Message = tableResults.Sum(item => item.CandidateCount) == 0
                ? "No historical projection gaps were found in the selected range."
                : "Historical projection gaps were summarized successfully.",
            ProcessedTableCount = tableResults.Count,
            CandidateCount = tableResults.Sum(item => item.CandidateCount),
            Tables = tableResults
        };
        logger.LogInformation(
            "Historical projection-gap preview completed. TableCount={TableCount}, CandidateCount={CandidateCount}",
            result.ProcessedTableCount,
            result.CandidateCount);
        return result;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public async Task<BusinessTaskProjectionBackfillResult> ExecuteAsync(
        BusinessTaskProjectionBackfillCommand command,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (command is null)
        {
            return BusinessTaskProjectionBackfillResult.Fail("Backfill command cannot be null.");
        }

        if (command.EndTimeLocal <= command.StartTimeLocal)
        {
            return BusinessTaskProjectionBackfillResult.Fail("EndTimeLocal must be greater than StartTimeLocal.");
        }

        if (command.MaxCount <= 0)
        {
            return BusinessTaskProjectionBackfillResult.Fail("MaxCount must be greater than 0.");
        }

        if (command.BatchSize <= 0)
        {
            return BusinessTaskProjectionBackfillResult.Fail("BatchSize must be greater than 0.");
        }

        var maxCount = Math.Min(command.MaxCount, MaxAllowedCount);
        var batchSize = Math.Min(command.BatchSize, MaxAllowedBatchSize);
        IReadOnlyList<SyncTableDefinition> definitions;
        try
        {
            definitions = await LoadDefinitionsAsync(command.TableCode, cancellationToken);
        }
        catch (InvalidOperationException exception) when (!string.IsNullOrWhiteSpace(command.TableCode))
        {
            return BusinessTaskProjectionBackfillResult.Fail(exception.Message);
        }

        var eligibleDefinitions = definitions
            .Where(IsEligibleDefinition)
            .OrderBy(definition => definition.TableCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (eligibleDefinitions.Count == 0)
        {
            return BusinessTaskProjectionBackfillResult.Fail("No eligible projection-backed table configuration was found.");
        }

        var tableResults = new List<BusinessTaskProjectionBackfillTableResult>();
        var remainingCount = maxCount;
        foreach (var definition in eligibleDefinitions)
        {
            if (remainingCount <= 0)
            {
                break;
            }

            var requireOrderId = !string.IsNullOrWhiteSpace(definition.OrderIdColumn);
            var requireStoreId = !string.IsNullOrWhiteSpace(definition.StoreIdColumn);
            var requireStoreName = !string.IsNullOrWhiteSpace(definition.StoreNameColumn);
            var requireProductCode = !string.IsNullOrWhiteSpace(definition.ProductCodeColumn);
            var requirePickLocation = !string.IsNullOrWhiteSpace(definition.PickLocationColumn);
            var candidates = await businessTaskRepository.FindProjectionBackfillCandidatesAsync(
                definition.TableCode,
                command.StartTimeLocal,
                command.EndTimeLocal,
                requireOrderId,
                requireStoreId,
                requireStoreName,
                requireProductCode,
                requirePickLocation,
                remainingCount,
                cancellationToken);
            if (candidates.Count == 0)
            {
                continue;
            }

            var requestedColumns = BuildRequestedColumns(definition);
            var candidateKeys = candidates
                .Select(candidate => candidate.BusinessKey)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var remoteRows = new List<IReadOnlyDictionary<string, object?>>();
            for (var offset = 0; offset < candidateKeys.Count; offset += batchSize)
            {
                var batchKeys = candidateKeys.Skip(offset).Take(batchSize).ToArray();
                if (batchKeys.Length == 0)
                {
                    continue;
                }

                var batchRows = await oracleSourceReader.ReadRowsByBusinessKeysAsync(
                    new OracleBusinessKeyRowReadRequest
                    {
                        TableCode = definition.TableCode,
                        SourceSchema = definition.SourceSchema,
                        SourceTable = definition.SourceTable,
                        BusinessKeyColumn = definition.BusinessKeyColumn,
                        RequestedColumns = requestedColumns,
                        BusinessKeys = batchKeys
                    },
                    cancellationToken);
                remoteRows.AddRange(batchRows);
            }

            var projectionRows = new List<BusinessTaskProjectionRow>(remoteRows.Count);
            var remoteKeySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in remoteRows)
            {
                var projectionRow = BuildProjectionRow(definition, row);
                if (projectionRow is null)
                {
                    continue;
                }

                projectionRows.Add(projectionRow);
                remoteKeySet.Add(projectionRow.BusinessKey);
            }

            var projectedEntities = businessTaskProjectionService.Project(new BusinessTaskProjectionRequest
            {
                Rows = projectionRows
            });
            var updatedCount = await businessTaskRepository.UpsertProjectionBatchAsync(projectedEntities.Entities, cancellationToken);
            var tableResult = new BusinessTaskProjectionBackfillTableResult
            {
                TableCode = definition.TableCode,
                CandidateCount = candidates.Count,
                RemoteRowCount = remoteRows.Count,
                ProjectedCount = projectedEntities.Entities.Count,
                UpdatedCount = updatedCount,
                MissingRemoteCount = Math.Max(candidateKeys.Count - remoteKeySet.Count, 0)
            };
            tableResults.Add(tableResult);
            remainingCount -= candidates.Count;
        }

        var result = new BusinessTaskProjectionBackfillResult
        {
            IsSuccess = true,
            Message = tableResults.Count == 0
                ? "No historical tasks required projection backfill in the selected range."
                : "Historical projection backfill completed.",
            ProcessedTableCount = tableResults.Count,
            CandidateCount = tableResults.Sum(item => item.CandidateCount),
            RemoteRowCount = tableResults.Sum(item => item.RemoteRowCount),
            ProjectedCount = tableResults.Sum(item => item.ProjectedCount),
            UpdatedCount = tableResults.Sum(item => item.UpdatedCount),
            MissingRemoteCount = tableResults.Sum(item => item.MissingRemoteCount),
            Tables = tableResults
        };
        logger.LogInformation(
            "Historical projection backfill completed. TableCount={TableCount}, CandidateCount={CandidateCount}, UpdatedCount={UpdatedCount}, MissingRemoteCount={MissingRemoteCount}",
            result.ProcessedTableCount,
            result.CandidateCount,
            result.UpdatedCount,
            result.MissingRemoteCount);
        return result;
    }

    private async Task<IReadOnlyList<SyncTableDefinition>> LoadDefinitionsAsync(string? tableCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tableCode))
        {
            var definition = await syncTaskConfigRepository.GetByTableCodeAsync(tableCode.Trim(), cancellationToken);
            return [definition];
        }

        return await syncTaskConfigRepository.ListEnabledAsync(cancellationToken);
    }

    private static bool IsEligibleDefinition(SyncTableDefinition definition)
    {
        return definition.Enabled
            && !string.IsNullOrWhiteSpace(definition.SourceSchema)
            && !string.IsNullOrWhiteSpace(definition.SourceTable)
            && !string.IsNullOrWhiteSpace(definition.CursorColumn)
            && !string.IsNullOrWhiteSpace(definition.BusinessKeyColumn)
            && definition.SourceType != BusinessTaskSourceType.Unknown
            && HasAnyProjectedField(definition);
    }

    private static bool HasAnyProjectedField(SyncTableDefinition definition)
    {
        return !string.IsNullOrWhiteSpace(definition.OrderIdColumn)
            || !string.IsNullOrWhiteSpace(definition.StoreIdColumn)
            || !string.IsNullOrWhiteSpace(definition.StoreNameColumn)
            || !string.IsNullOrWhiteSpace(definition.ProductCodeColumn)
            || !string.IsNullOrWhiteSpace(definition.PickLocationColumn);
    }

    private static IReadOnlyList<string> BuildRequestedColumns(SyncTableDefinition definition)
    {
        return new[]
            {
                definition.BusinessKeyColumn,
                definition.CursorColumn,
                definition.BarcodeColumn,
                definition.WaveCodeColumn,
                definition.WaveRemarkColumn,
                definition.WorkingAreaColumn,
                definition.OrderIdColumn,
                definition.StoreIdColumn,
                definition.StoreNameColumn,
                definition.ProductCodeColumn,
                definition.PickLocationColumn
            }
            .Where(column => !string.IsNullOrWhiteSpace(column))
            .Select(column => column!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static BusinessTaskProjectionRow? BuildProjectionRow(
        SyncTableDefinition definition,
        IReadOnlyDictionary<string, object?> row)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!TryReadNonEmptyString(row, definition.BusinessKeyColumn, out var businessKey))
        {
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
        var projectedTimeLocal = ResolveProjectedTimeLocal(definition, businessKey, row);
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
    /// 执行当前方法。
    /// </summary>
    private static DateTime ResolveProjectedTimeLocal(
        SyncTableDefinition definition,
        string businessKey,
        IReadOnlyDictionary<string, object?> row)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!row.TryGetValue(definition.CursorColumn, out var rawValue) || rawValue is null)
        {
            throw new InvalidOperationException(
                $"Projected time column is missing. TableCode={definition.TableCode}, CursorColumn={definition.CursorColumn}, BusinessKey={businessKey}");
        }

        if (!TryConvertToLocalDateTime(rawValue, out var localTime))
        {
            throw new InvalidOperationException(
                $"Projected time value is invalid. TableCode={definition.TableCode}, CursorColumn={definition.CursorColumn}, BusinessKey={businessKey}");
        }

        return localTime;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static bool TryReadNonEmptyString(
        IReadOnlyDictionary<string, object?> row,
        string columnName,
        out string value)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return false;
        }

        if (!row.TryGetValue(columnName, out var rawValue) || rawValue is null)
        {
            return false;
        }

        var text = rawValue.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text;
        return true;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static bool TryReadOptionalString(
        IReadOnlyDictionary<string, object?> row,
        string? columnName,
        out string? value)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        value = null;
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return false;
        }

        if (!row.TryGetValue(columnName, out var rawValue) || rawValue is null)
        {
            return false;
        }

        var text = rawValue.ToString()?.Trim();
        value = string.IsNullOrWhiteSpace(text) ? null : text;
        return true;
    }

    private static bool TryConvertToLocalDateTime(object rawValue, out DateTime localTime)
    {
        localTime = default;
        try
        {
            switch (rawValue)
            {
                case DateTime dateTime:
                    if (dateTime.Kind == DateTimeKind.Local)
                    {
                        localTime = dateTime;
                        return true;
                    }

                    if (dateTime.Kind == DateTimeKind.Unspecified)
                    {
                        localTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                        return true;
                    }

                    return false;
                case DateTimeOffset:
                    return false;
                default:
                    var converted = Convert.ToDateTime(rawValue);
                    if (converted.Kind == DateTimeKind.Local)
                    {
                        localTime = converted;
                        return true;
                    }

                    if (converted.Kind == DateTimeKind.Unspecified)
                    {
                        localTime = DateTime.SpecifyKind(converted, DateTimeKind.Local);
                        return true;
                    }

                    return false;
            }
        }
        catch
        {
            return false;
        }
    }
}

