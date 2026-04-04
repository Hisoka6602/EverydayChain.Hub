using System.Globalization;
using System.Text.RegularExpressions;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步配置仓储实现。
/// 所有表定义在构造时一次性解析并索引到字典，后续查询为 O(1) 访问，不重复遍历配置列表。
/// </summary>
public class SyncTaskConfigRepository : ISyncTaskConfigRepository
{
    /// <summary>时间偏移或 UTC 标记检测正则。</summary>
    private static readonly Regex UtcOrOffsetRegex = new(@"(?:Z|[+\-]\d{2}:\d{2}|[+\-]\d{4})\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>同步配置快照。</summary>
    private readonly SyncJobOptions _options;

    /// <summary>最大并行表数量（已应用默认值）。</summary>
    private readonly int _maxParallelTables;

    /// <summary>按表编码的定义字典（忽略大小写），启动时一次性构建，后续 O(1) 查询。</summary>
    private readonly IReadOnlyDictionary<string, SyncTableDefinition> _definitionIndex;

    /// <summary>已启用的表定义列表，启动时一次性构建。</summary>
    private readonly IReadOnlyList<SyncTableDefinition> _enabledDefinitions;

    /// <summary>
    /// 初始化同步配置仓储，在构造阶段完成所有表定义的解析与验证。
    /// </summary>
    /// <param name="syncJobOptions">同步任务配置。</param>
    /// <param name="logger">日志记录器。</param>
    public SyncTaskConfigRepository(IOptions<SyncJobOptions> syncJobOptions, ILogger<SyncTaskConfigRepository> logger)
    {
        _options = syncJobOptions.Value;
        _maxParallelTables = _options.MaxParallelTables > 0 ? _options.MaxParallelTables : 1;

        // 启动时一次性解析并验证所有表定义，后续查询无需重复解析。
        var index = new Dictionary<string, SyncTableDefinition>(StringComparer.OrdinalIgnoreCase);
        var enabled = new List<SyncTableDefinition>();
        foreach (var table in _options.Tables)
        {
            var definition = MapDefinition(table, logger);
            index[table.TableCode] = definition;
            if (table.Enabled)
            {
                enabled.Add(definition);
            }
        }

        _definitionIndex = index;
        _enabledDefinitions = enabled;
    }

    /// <inheritdoc/>
    public Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct)
    {
        if (_definitionIndex.TryGetValue(tableCode, out var definition))
        {
            return Task.FromResult(definition);
        }

        throw new InvalidOperationException($"未找到同步表配置: {tableCode}");
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct)
    {
        return Task.FromResult(_enabledDefinitions);
    }

    /// <inheritdoc/>
    public Task<int> GetMaxParallelTablesAsync(CancellationToken ct)
    {
        return Task.FromResult(_maxParallelTables);
    }

    /// <summary>
    /// 映射单表配置到领域定义。
    /// </summary>
    /// <param name="table">单表配置。</param>
    /// <param name="logger">日志记录器。</param>
    /// <returns>领域定义。</returns>
    private SyncTableDefinition MapDefinition(SyncTableOptions table, ILogger logger)
    {
        var startTimeLocal = ParseLocalTime(table.StartTimeLocal, table.TableCode, logger);
        ValidateExcludedColumns(table);
        var globalPollingIntervalSeconds = _options.PollingIntervalSeconds > 0 ? _options.PollingIntervalSeconds : 60;
        var effectivePageSize = table.PageSize > 0 ? table.PageSize : 5000;
        var priority = ParsePriority(table.Priority, table.TableCode);
        return new SyncTableDefinition
        {
            TableCode = table.TableCode,
            Enabled = table.Enabled,
            SyncMode = SyncMode.Incremental,
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
        };
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
        var conflictsWithUniqueKeys = uniqueKeys.Where(excludedColumns.Contains).ToList();
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

        var conflictsWithSoftDeleteColumns = SyncColumnFilter.NormalizedSoftDeleteColumns.Where(excludedColumns.Contains).ToList();
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
    /// <param name="logger">日志记录器。</param>
    /// <returns>本地时间。</returns>
    private static DateTime ParseLocalTime(string localTimeText, string tableCode, ILogger logger)
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
