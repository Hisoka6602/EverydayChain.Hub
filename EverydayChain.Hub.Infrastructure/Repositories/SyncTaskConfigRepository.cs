using System.Globalization;
using System.Text.RegularExpressions;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义 SyncTaskConfigRepository 类型。
/// </summary>
public class SyncTaskConfigRepository(IOptions<SyncJobOptions> syncJobOptions, ILogger<SyncTaskConfigRepository> logger) : ISyncTaskConfigRepository
{
    private static readonly Regex UtcOrOffsetRegex = new(@"(?:Z|[+\-]\d{2}:\d{2}|[+\-]\d{4})\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly SyncJobOptions _options = syncJobOptions.Value;

    public Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct)
    {
        var table = _options.Tables.FirstOrDefault(x => string.Equals(x.TableCode, tableCode, StringComparison.OrdinalIgnoreCase));
        if (table is null)
        {
            throw new InvalidOperationException($"Sync table configuration was not found: {tableCode}");
        }

        return Task.FromResult(MapDefinition(table));
    }

    public Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct)
    {
        var definitions = _options.Tables.Where(x => x.Enabled).Select(MapDefinition).ToList();
        return Task.FromResult<IReadOnlyList<SyncTableDefinition>>(definitions);
    }

    public Task<int> GetMaxParallelTablesAsync(CancellationToken ct)
    {
        var maxParallelTables = _options.MaxParallelTables > 0 ? _options.MaxParallelTables : 1;
        return Task.FromResult(maxParallelTables);
    }

    private SyncTableDefinition MapDefinition(SyncTableOptions table)
    {
        var startTimeLocal = ParseLocalTime(table.StartTimeLocal, table.TableCode);
        ValidateExcludedColumns(table);
        var syncMode = ParseSyncMode(table.SyncMode, table.TableCode);
        var globalPollingIntervalSeconds = _options.PollingIntervalSeconds > 0 ? _options.PollingIntervalSeconds : 60;
        var effectivePageSize = table.PageSize > 0 ? table.PageSize : 5000;
        var priority = ParsePriority(table.Priority, table.TableCode);
        var statusConsumeProfile = BuildStatusConsumeProfile(table, syncMode);
        var sourceType = ParseSourceType(table.SourceType, table.TableCode);
        var businessKeyColumn = NormalizeAndValidateOptionalIdentifier(table.BusinessKeyColumn, table.TableCode, nameof(table.BusinessKeyColumn)) ?? string.Empty;
        var barcodeColumn = NormalizeAndValidateOptionalIdentifier(table.BarcodeColumn, table.TableCode, nameof(table.BarcodeColumn));
        var waveCodeColumn = NormalizeAndValidateOptionalIdentifier(table.WaveCodeColumn, table.TableCode, nameof(table.WaveCodeColumn));
        var waveRemarkColumn = NormalizeAndValidateOptionalIdentifier(table.WaveRemarkColumn, table.TableCode, nameof(table.WaveRemarkColumn));
        var workingAreaColumn = NormalizeAndValidateOptionalIdentifier(table.WorkingAreaColumn, table.TableCode, nameof(table.WorkingAreaColumn));
        var orderIdColumn = NormalizeAndValidateOptionalIdentifier(table.OrderIdColumn, table.TableCode, nameof(table.OrderIdColumn));
        var storeIdColumn = NormalizeAndValidateOptionalIdentifier(table.StoreIdColumn, table.TableCode, nameof(table.StoreIdColumn));
        var storeNameColumn = NormalizeAndValidateOptionalIdentifier(table.StoreNameColumn, table.TableCode, nameof(table.StoreNameColumn));
        var productCodeColumn = NormalizeAndValidateOptionalIdentifier(table.ProductCodeColumn, table.TableCode, nameof(table.ProductCodeColumn));
        var pickLocationColumn = NormalizeAndValidateOptionalIdentifier(table.PickLocationColumn, table.TableCode, nameof(table.PickLocationColumn));

        return new SyncTableDefinition
        {
            TableCode = table.TableCode,
            Enabled = table.Enabled,
            SyncMode = syncMode,
            SourceSchema = table.SourceSchema,
            SourceTable = table.SourceTable,
            TargetLogicalTable = table.TargetLogicalTable,
            CursorColumn = table.CursorColumn,
            StartTimeLocal = startTimeLocal,
            PollingIntervalSeconds = table.PollingIntervalSeconds > 0 ? table.PollingIntervalSeconds : globalPollingIntervalSeconds,
            MaxLagMinutes = table.MaxLagMinutes > 0 ? table.MaxLagMinutes : _options.DefaultMaxLagMinutes,
            Priority = priority,
            PageSize = effectivePageSize,
            UniqueKeys = table.UniqueKeys,
            ExcludedColumns = table.ExcludedColumns,
            DeletionPolicy = table.Delete.Policy,
            DeletionEnabled = table.Delete.Enabled,
            DeletionDryRun = table.Delete.DryRun,
            DeletionCompareSegmentSize = table.Delete.CompareSegmentSize > 0 ? table.Delete.CompareSegmentSize : 20000,
            DeletionCompareMaxParallelism = table.Delete.CompareMaxParallelism > 0 ? table.Delete.CompareMaxParallelism : 4,
            RetentionEnabled = table.Retention.Enabled,
            RetentionKeepMonths = table.Retention.KeepMonths > 0 ? table.Retention.KeepMonths : 3,
            RetentionDryRun = table.Retention.DryRun,
            RetentionAllowDrop = table.Retention.AllowDrop,
            StatusConsumeProfile = statusConsumeProfile,
            SourceType = sourceType,
            BusinessKeyColumn = businessKeyColumn,
            BarcodeColumn = barcodeColumn,
            WaveCodeColumn = waveCodeColumn,
            WaveRemarkColumn = waveRemarkColumn,
            WorkingAreaColumn = workingAreaColumn,
            OrderIdColumn = orderIdColumn,
            StoreIdColumn = storeIdColumn,
            StoreNameColumn = storeNameColumn,
            ProductCodeColumn = productCodeColumn,
            PickLocationColumn = pickLocationColumn,
        };
    }

    private static BusinessTaskSourceType ParseSourceType(string? sourceTypeText, string tableCode)
    {
        if (string.IsNullOrWhiteSpace(sourceTypeText))
        {
            return BusinessTaskSourceType.Unknown;
        }

        var normalized = sourceTypeText.Trim();
        if (!Enum.TryParse<BusinessTaskSourceType>(normalized, true, out var sourceType) || !Enum.IsDefined(sourceType))
        {
            throw new InvalidOperationException($"Table {tableCode} has an invalid SourceType value. Only Split or FullCase are supported.");
        }

        return sourceType;
    }

    private static SyncMode ParseSyncMode(string? syncModeText, string tableCode)
    {
        if (string.IsNullOrWhiteSpace(syncModeText))
        {
            return SyncMode.KeyedMerge;
        }

        var normalized = syncModeText.Trim();
        if (!Enum.TryParse<SyncMode>(normalized, true, out var mode) || !Enum.IsDefined(mode))
        {
            throw new InvalidOperationException(
                $"Table {tableCode} has an invalid SyncMode value. Only {nameof(SyncMode.KeyedMerge)} or {nameof(SyncMode.StatusDriven)} are supported.");
        }

        return mode;
    }

    /// <summary>
    /// 构建状态驱动消费配置。
    /// </summary>
    /// <param name="table">同步表配置。</param>
    /// <param name="syncMode">同步模式。</param>
    /// <returns>状态驱动消费配置；非状态驱动模式时返回空。</returns>
    private RemoteStatusConsumeProfile? BuildStatusConsumeProfile(SyncTableOptions table, SyncMode syncMode)
    {
        if (syncMode != SyncMode.StatusDriven)
        {
            return null;
        }

        var statusColumnName = string.IsNullOrWhiteSpace(table.StatusColumnName) ? "TASKPROCESS" : table.StatusColumnName.Trim();
        EnsureSafeIdentifier(statusColumnName, table.TableCode, nameof(table.StatusColumnName));
        var batchSize = table.StatusBatchSize > 0 ? table.StatusBatchSize : 5000;
        var completedStatusValue = string.IsNullOrWhiteSpace(table.CompletedStatusValue) ? "Y" : table.CompletedStatusValue.Trim();
        var writeBackCompletedTimeColumnName = NormalizeAndValidateOptionalIdentifier(table.WriteBackCompletedTimeColumnName, table.TableCode, nameof(table.WriteBackCompletedTimeColumnName));
        var writeBackBatchIdColumnName = NormalizeAndValidateOptionalIdentifier(table.WriteBackBatchIdColumnName, table.TableCode, nameof(table.WriteBackBatchIdColumnName));

        string? pendingStatusValue;
        if (table.PendingStatusValue is null)
        {
            pendingStatusValue = null;
        }
        else
        {
            pendingStatusValue = table.PendingStatusValue.Trim();
            if (pendingStatusValue.Length == 0)
            {
                // 步骤：兼容配置绑定把显式 null 映射为空字符串的场景，按 null 语义处理并输出告警。
                logger.LogWarning(
                    "同步表配置检测到空白 PendingStatusValue，已按 null 语义处理。TableCode={TableCode}",
                    table.TableCode);
                pendingStatusValue = null;
            }
        }

        return new RemoteStatusConsumeProfile
        {
            StatusColumnName = statusColumnName,
            PendingStatusValue = pendingStatusValue,
            CompletedStatusValue = completedStatusValue,
            ShouldWriteBackRemoteStatus = table.ShouldWriteBackRemoteStatus,
            BatchSize = batchSize,
            WriteBackCompletedTimeColumnName = writeBackCompletedTimeColumnName,
            WriteBackBatchIdColumnName = writeBackBatchIdColumnName,
        };
    }

    private static string? NormalizeAndValidateOptionalIdentifier(string? identifierText, string tableCode, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(identifierText))
        {
            return null;
        }

        var normalized = identifierText.Trim();
        EnsureSafeIdentifier(normalized, tableCode, fieldName);
        return normalized;
    }

    private static void EnsureSafeIdentifier(string identifier, string tableCode, string fieldName)
    {
        if (!identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            throw new InvalidOperationException($"Table {tableCode} has an invalid {fieldName} value. Only letters, digits, and underscores are allowed.");
        }
    }

    private static void ValidateExcludedColumns(SyncTableOptions table)
    {
        var excludedColumns = SyncColumnFilter.NormalizeColumns(table.ExcludedColumns);
        if (excludedColumns.Count == 0)
        {
            return;
        }

        var uniqueKeys = SyncColumnFilter.NormalizeColumns(table.UniqueKeys);
        var conflictsWithUniqueKeys = uniqueKeys.Intersect(excludedColumns, StringComparer.OrdinalIgnoreCase).ToList();
        if (conflictsWithUniqueKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Table {table.TableCode} has ExcludedColumns that conflict with UniqueKeys: {string.Join(", ", conflictsWithUniqueKeys)}.");
        }

        var normalizedCursorColumn = SyncColumnFilter.NormalizeColumnName(table.CursorColumn);
        if (!string.IsNullOrWhiteSpace(normalizedCursorColumn) && excludedColumns.Contains(normalizedCursorColumn))
        {
            throw new InvalidOperationException($"Table {table.TableCode} cannot include CursorColumn in ExcludedColumns: {table.CursorColumn}.");
        }

        var conflictsWithSoftDeleteColumns = SyncColumnFilter.SoftDeleteColumns
            .Intersect(excludedColumns, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (conflictsWithSoftDeleteColumns.Count > 0)
        {
            throw new InvalidOperationException(
                $"Table {table.TableCode} cannot exclude soft-delete marker columns: {string.Join(", ", conflictsWithSoftDeleteColumns)}.");
        }
    }

    private DateTime ParseLocalTime(string localTimeText, string tableCode)
    {
        if (string.IsNullOrWhiteSpace(localTimeText))
        {
            throw new InvalidOperationException($"Table {tableCode} must define StartTimeLocal.");
        }

        if (UtcOrOffsetRegex.IsMatch(localTimeText))
        {
            throw new InvalidOperationException($"Table {tableCode} must use a local StartTimeLocal value without UTC or offset markers.");
        }

        if (!DateTime.TryParse(localTimeText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            throw new InvalidOperationException($"Table {tableCode} has an invalid StartTimeLocal value.");
        }

        var localTime = DateTime.SpecifyKind(parsed, DateTimeKind.Local);
        logger.LogInformation("Loaded sync table configuration. TableCode={TableCode}, StartTimeLocal={StartTimeLocal}", tableCode, localTime);
        return localTime;
    }

    private static SyncTablePriority ParsePriority(string? priorityText, string tableCode)
    {
        if (string.IsNullOrWhiteSpace(priorityText))
        {
            return SyncTablePriority.Low;
        }

        var normalized = priorityText.Trim();
        if (normalized.Equals(nameof(SyncTablePriority.High), StringComparison.OrdinalIgnoreCase))
        {
            return SyncTablePriority.High;
        }

        if (normalized.Equals(nameof(SyncTablePriority.Low), StringComparison.OrdinalIgnoreCase))
        {
            return SyncTablePriority.Low;
        }

        throw new InvalidOperationException($"Table {tableCode} has an invalid Priority value. Only High or Low are supported.");
    }
}

