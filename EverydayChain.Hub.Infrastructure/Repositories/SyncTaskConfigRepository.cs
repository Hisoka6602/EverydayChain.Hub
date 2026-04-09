using System.Globalization;
using System.Text.RegularExpressions;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步配置仓储实现。
/// </summary>
public class SyncTaskConfigRepository(IOptions<SyncJobOptions> syncJobOptions, ILogger<SyncTaskConfigRepository> logger) : ISyncTaskConfigRepository
{
    /// <summary>时间偏移或 UTC 标记检测正则。</summary>
    private static readonly Regex UtcOrOffsetRegex = new(@"(?:Z|[+\-]\d{2}:\d{2}|[+\-]\d{4})\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>同步配置快照。</summary>
    private readonly SyncJobOptions _options = syncJobOptions.Value;

    /// <inheritdoc/>
    public Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct)
    {
        var table = _options.Tables.FirstOrDefault(x => string.Equals(x.TableCode, tableCode, StringComparison.OrdinalIgnoreCase));
        if (table is null)
        {
            throw new InvalidOperationException($"未找到同步表配置: {tableCode}");
        }

        return Task.FromResult(MapDefinition(table));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct)
    {
        var definitions = _options.Tables.Where(x => x.Enabled).Select(MapDefinition).ToList();
        return Task.FromResult<IReadOnlyList<SyncTableDefinition>>(definitions);
    }

    /// <inheritdoc/>
    public Task<int> GetMaxParallelTablesAsync(CancellationToken ct)
    {
        var maxParallelTables = _options.MaxParallelTables > 0 ? _options.MaxParallelTables : 1;
        return Task.FromResult(maxParallelTables);
    }

    /// <summary>
    /// 映射单表配置到领域定义。
    /// </summary>
    /// <param name="table">单表配置。</param>
    /// <returns>领域定义。</returns>
    private SyncTableDefinition MapDefinition(SyncTableOptions table)
    {
        var startTimeLocal = ParseLocalTime(table.StartTimeLocal, table.TableCode);
        ValidateExcludedColumns(table);
        var syncMode = ParseSyncMode(table.SyncMode, table.TableCode);
        var globalPollingIntervalSeconds = _options.PollingIntervalSeconds > 0 ? _options.PollingIntervalSeconds : 60;
        var effectivePageSize = table.PageSize > 0 ? table.PageSize : 5000;
        var priority = ParsePriority(table.Priority, table.TableCode);
        var statusConsumeProfile = BuildStatusConsumeProfile(table, syncMode);
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
        };
    }

    /// <summary>
    /// 解析同步模式配置。
    /// </summary>
    /// <param name="syncModeText">同步模式文本。</param>
    /// <param name="tableCode">表编码。</param>
    /// <returns>同步模式。</returns>
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
                $"表 {tableCode} 的 SyncMode 配置非法，仅支持 {nameof(SyncMode.KeyedMerge)} 或 {nameof(SyncMode.StatusDriven)}。");
        }

        return mode;
    }

    /// <summary>
    /// 构建状态驱动消费配置。
    /// </summary>
    /// <param name="table">单表配置。</param>
    /// <param name="syncMode">同步模式。</param>
    /// <returns>状态消费配置；非 StatusDriven 模式返回 null。</returns>
    private static RemoteStatusConsumeProfile? BuildStatusConsumeProfile(SyncTableOptions table, SyncMode syncMode)
    {
        if (syncMode != SyncMode.StatusDriven)
        {
            return null;
        }

        var statusColumnName = string.IsNullOrWhiteSpace(table.StatusColumnName) ? "TASKPROCESS" : table.StatusColumnName.Trim();
        EnsureSafeIdentifier(statusColumnName, table.TableCode, nameof(table.StatusColumnName));
        var batchSize = table.StatusBatchSize > 0 ? table.StatusBatchSize : 5000;
        var completedStatusValue = string.IsNullOrWhiteSpace(table.CompletedStatusValue) ? "Y" : table.CompletedStatusValue.Trim();
        if (table.ShouldWriteBackRemoteStatus && string.IsNullOrWhiteSpace(completedStatusValue))
        {
            throw new InvalidOperationException($"表 {table.TableCode} 开启远端回写时，CompletedStatusValue 不能为空。");
        }

        return new RemoteStatusConsumeProfile
        {
            StatusColumnName = statusColumnName,
            PendingStatusValue = table.PendingStatusValue,
            CompletedStatusValue = completedStatusValue,
            ShouldWriteBackRemoteStatus = table.ShouldWriteBackRemoteStatus,
            BatchSize = batchSize,
        };
    }

    /// <summary>
    /// 校验标识符安全性。
    /// </summary>
    /// <param name="identifier">标识符文本。</param>
    /// <param name="tableCode">表编码。</param>
    /// <param name="fieldName">字段名。</param>
    private static void EnsureSafeIdentifier(string identifier, string tableCode, string fieldName)
    {
        if (!identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            throw new InvalidOperationException($"表 {tableCode} 的 {fieldName} 包含非法字符，仅允许字母、数字、下划线。");
        }
    }

    /// <summary>
    /// 校验排除列关键约束。
    /// </summary>
    /// <param name="table">单表配置。</param>
    /// <exception cref="InvalidOperationException">当排除列与关键控制列冲突时抛出。</exception>
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
                $"表 {table.TableCode} 的 ExcludedColumns 与 UniqueKeys 冲突：{string.Join(", ", conflictsWithUniqueKeys)}。");
        }

        var normalizedCursorColumn = SyncColumnFilter.NormalizeColumnName(table.CursorColumn);
        if (!string.IsNullOrWhiteSpace(normalizedCursorColumn) && excludedColumns.Contains(normalizedCursorColumn))
        {
            throw new InvalidOperationException($"表 {table.TableCode} 的 ExcludedColumns 禁止包含 CursorColumn：{table.CursorColumn}。");
        }

        var conflictsWithSoftDeleteColumns = SyncColumnFilter.NormalizedSoftDeleteColumns
            .Intersect(excludedColumns, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (conflictsWithSoftDeleteColumns.Count > 0)
        {
            throw new InvalidOperationException(
                $"表 {table.TableCode} 的 ExcludedColumns 禁止包含软删除标记列：{string.Join(", ", conflictsWithSoftDeleteColumns)}。");
        }
    }

    /// <summary>
    /// 解析本地时间字符串。
    /// </summary>
    /// <param name="localTimeText">时间文本。</param>
    /// <param name="tableCode">表编码。</param>
    /// <returns>本地时间。</returns>
    private DateTime ParseLocalTime(string localTimeText, string tableCode)
    {
        if (string.IsNullOrWhiteSpace(localTimeText))
        {
            throw new InvalidOperationException($"表 {tableCode} 的 StartTimeLocal 不能为空。");
        }

        if (UtcOrOffsetRegex.IsMatch(localTimeText))
        {
            throw new InvalidOperationException($"表 {tableCode} 的 StartTimeLocal 仅允许本地时间字符串，禁止 Z 或 offset。");
        }

        if (!DateTime.TryParse(localTimeText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            throw new InvalidOperationException($"表 {tableCode} 的 StartTimeLocal 无法解析。");
        }

        var localTime = DateTime.SpecifyKind(parsed, DateTimeKind.Local);
        logger.LogInformation("加载同步配置成功。TableCode={TableCode}, StartTimeLocal={StartTimeLocal}", tableCode, localTime);
        return localTime;
    }

    /// <summary>
    /// 解析同步优先级配置。
    /// </summary>
    /// <param name="priorityText">优先级文本。</param>
    /// <param name="tableCode">表编码。</param>
    /// <returns>优先级枚举。</returns>
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

        throw new InvalidOperationException($"表 {tableCode} 的 Priority 配置非法，仅支持 High 或 Low。");
    }
}
