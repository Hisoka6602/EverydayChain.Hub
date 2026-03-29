using System.Globalization;
using System.Text.RegularExpressions;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    /// <summary>
    /// 映射单表配置到领域定义。
    /// </summary>
    /// <param name="table">单表配置。</param>
    /// <returns>领域定义。</returns>
    private SyncTableDefinition MapDefinition(SyncTableOptions table)
    {
        var startTimeLocal = ParseLocalTime(table.StartTimeLocal, table.TableCode);
        ValidateExcludedColumns(table);
        var globalPollingIntervalSeconds = _options.PollingIntervalSeconds > 0 ? _options.PollingIntervalSeconds : 60;
        var effectivePageSize = table.PageSize > 0 ? table.PageSize : 5000;
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
            PageSize = effectivePageSize,
            UniqueKeys = table.UniqueKeys,
            ExcludedColumns = table.ExcludedColumns,
            DeletionPolicy = table.Delete.Policy,
            DeletionEnabled = table.Delete.Enabled,
            DeletionDryRun = table.Delete.DryRun,
            DeletionCompareSegmentSize = table.Delete.CompareSegmentSize > 0 ? table.Delete.CompareSegmentSize : 20000,
            DeletionCompareMaxParallelism = table.Delete.CompareMaxParallelism > 0 ? table.Delete.CompareMaxParallelism : 4,
        };
    }

    /// <summary>
    /// 校验排除列关键约束。
    /// </summary>
    /// <param name="table">单表配置。</param>
    /// <exception cref="InvalidOperationException">当排除列与关键控制列冲突时抛出。</exception>
    private static void ValidateExcludedColumns(SyncTableOptions table)
    {
        var excludedColumns = NormalizeColumns(table.ExcludedColumns);
        if (excludedColumns.Count == 0)
        {
            return;
        }

        var uniqueKeys = NormalizeColumns(table.UniqueKeys);
        var conflictsWithUniqueKeys = uniqueKeys.Where(excludedColumns.Contains).ToList();
        if (conflictsWithUniqueKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"表 {table.TableCode} 的 ExcludedColumns 与 UniqueKeys 冲突：{string.Join(", ", conflictsWithUniqueKeys)}。");
        }

        if (!string.IsNullOrWhiteSpace(table.CursorColumn) && excludedColumns.Contains(table.CursorColumn.Trim()))
        {
            throw new InvalidOperationException($"表 {table.TableCode} 的 ExcludedColumns 禁止包含 CursorColumn：{table.CursorColumn}。");
        }

        var softDeleteColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IsDeleted",
            "DeletedTimeLocal",
        };
        var conflictsWithSoftDeleteColumns = softDeleteColumns.Where(excludedColumns.Contains).ToList();
        if (conflictsWithSoftDeleteColumns.Count > 0)
        {
            throw new InvalidOperationException(
                $"表 {table.TableCode} 的 ExcludedColumns 禁止包含软删除标记列：{string.Join(", ", conflictsWithSoftDeleteColumns)}。");
        }
    }

    /// <summary>
    /// 规范化列名集合并按忽略大小写去重。
    /// </summary>
    /// <param name="columns">原始列名集合。</param>
    /// <returns>规范化后的列名集合。</returns>
    private static HashSet<string> NormalizeColumns(IEnumerable<string> columns)
    {
        return columns
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
}
