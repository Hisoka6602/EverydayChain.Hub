using System.Globalization;
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
            PollingIntervalSeconds = table.PollingIntervalSeconds > 0 ? table.PollingIntervalSeconds : _options.PollingIntervalSeconds,
            MaxLagMinutes = table.MaxLagMinutes > 0 ? table.MaxLagMinutes : _options.DefaultMaxLagMinutes,
            PageSize = table.PageSize,
            UniqueKeys = table.UniqueKeys,
            ExcludedColumns = table.ExcludedColumns,
        };
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

        if (localTimeText.Contains('Z', StringComparison.OrdinalIgnoreCase) || localTimeText.Contains('+'))
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
