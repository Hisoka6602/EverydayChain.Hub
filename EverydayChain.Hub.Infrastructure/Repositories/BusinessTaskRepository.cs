using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 业务任务仓储 EF Core 实现，按月写入与查询 <c>business_tasks_{yyyyMM}</c> 分表。
/// </summary>
public class BusinessTaskRepository(
    IDbContextFactory<HubDbContext> contextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IShardTableResolver shardTableResolver,
    IOptions<ShardingOptions> shardingOptions) : IBusinessTaskRepository
{
    /// <summary>业务任务逻辑表名。</summary>
    private const string BusinessTaskLogicalTable = "business_tasks";
    /// <summary>无波次占位文本。</summary>
    private const string EmptyWaveCode = "未分波次";
    /// <summary>无码头占位文本。</summary>
    private const string EmptyDockCode = "未分配码头";

    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _shardingOptions = shardingOptions.Value;

    /// <inheritdoc/>
    public async Task<BusinessTaskEntity?> FindByBarcodeAsync(string barcode, CancellationToken ct)
    {
        var normalizedBarcode = NormalizeOptionalText(barcode);
        if (string.IsNullOrWhiteSpace(normalizedBarcode))
        {
            return null;
        }

        return await FindFirstAcrossShardsAsync(query => query
            .Where(x => x.NormalizedBarcode == normalizedBarcode)
            .OrderByDescending(x => x.CreatedTimeLocal), ct);
    }

    /// <inheritdoc/>
    public async Task<BusinessTaskEntity?> FindByTaskCodeAsync(string taskCode, CancellationToken ct)
    {
        return await FindFirstAcrossShardsAsync(query => query
            .Where(x => x.TaskCode == taskCode)
            .OrderByDescending(x => x.CreatedTimeLocal), ct);
    }

    /// <inheritdoc/>
    public async Task<BusinessTaskEntity?> FindBySourceTableAndBusinessKeyAsync(string sourceTableCode, string businessKey, CancellationToken ct)
    {
        var normalizedSourceTableCode = NormalizeOptionalText(sourceTableCode);
        var normalizedBusinessKey = NormalizeOptionalText(businessKey);
        if (string.IsNullOrWhiteSpace(normalizedSourceTableCode) || string.IsNullOrWhiteSpace(normalizedBusinessKey))
        {
            return null;
        }

        return await FindFirstAcrossShardsAsync(query => query
            .Where(x => x.SourceTableCode == normalizedSourceTableCode && x.BusinessKey == normalizedBusinessKey)
            .OrderByDescending(x => x.CreatedTimeLocal), ct);
    }

    /// <inheritdoc/>
    public async Task<BusinessTaskEntity?> FindByIdAsync(long id, CancellationToken ct)
    {
        return await FindFirstAcrossShardsAsync(query => query.Where(x => x.Id == id), ct);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        entity.RefreshQueryFields();
        var suffix = shardSuffixResolver.ResolveLocal(entity.CreatedTimeLocal);
        await shardTableProvisioner.EnsureShardTableAsync(BusinessTaskLogicalTable, suffix, ct);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.BusinessTasks.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpsertProjectionAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        await UpsertProjectionBatchAsync([entity], ct);
    }

    /// <inheritdoc/>
    public async Task<int> UpsertProjectionBatchAsync(IReadOnlyList<BusinessTaskEntity> entities, CancellationToken ct)
    {
        if (entities.Count == 0)
        {
            return 0;
        }

        var uniqueEntitiesByKey = new Dictionary<ProjectionKey, BusinessTaskEntity>(entities.Count);
        foreach (var entity in entities)
        {
            entity.RefreshQueryFields();
            var key = ProjectionKey.Create(entity.SourceTableCode, entity.BusinessKey);
            if (key is null)
            {
                continue;
            }

            uniqueEntitiesByKey[key.Value] = entity;
        }

        if (uniqueEntitiesByKey.Count == 0)
        {
            return 0;
        }

        var sourceTableCodes = uniqueEntitiesByKey.Keys
            .Select(key => key.SourceTableCode)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var businessKeys = uniqueEntitiesByKey.Keys
            .Select(key => key.BusinessKey)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var existingByKey = await LoadProjectionExistingMapAsync(uniqueEntitiesByKey.Keys, sourceTableCodes, businessKeys, ct);

        var updateTargetsBySuffix = new Dictionary<string, List<ProjectionUpdateTarget>>(StringComparer.Ordinal);
        var insertTargetsBySuffix = new Dictionary<string, List<BusinessTaskEntity>>(StringComparer.Ordinal);
        foreach (var pair in uniqueEntitiesByKey)
        {
            if (existingByKey.TryGetValue(pair.Key, out var existing))
            {
                if (!updateTargetsBySuffix.TryGetValue(existing.Suffix, out var updates))
                {
                    updates = [];
                    updateTargetsBySuffix[existing.Suffix] = updates;
                }

                updates.Add(new ProjectionUpdateTarget(existing.Entity.Id, pair.Value));
                continue;
            }

            var insertSuffix = shardSuffixResolver.ResolveLocal(pair.Value.CreatedTimeLocal);
            if (!insertTargetsBySuffix.TryGetValue(insertSuffix, out var inserts))
            {
                inserts = [];
                insertTargetsBySuffix[insertSuffix] = inserts;
            }

            inserts.Add(pair.Value);
        }

        foreach (var pair in updateTargetsBySuffix)
        {
            using var scope = TableSuffixScope.Use(pair.Key);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var ids = pair.Value.Select(target => target.Id).Distinct().ToArray();
            var trackedById = await db.BusinessTasks
                .Where(task => ids.Contains(task.Id))
                .ToDictionaryAsync(task => task.Id, ct);
            foreach (var updateTarget in pair.Value)
            {
                if (!trackedById.TryGetValue(updateTarget.Id, out var tracked))
                {
                    continue;
                }

                MergeProjectionFields(tracked, updateTarget.Incoming);
            }

            await db.SaveChangesAsync(ct);
        }

        foreach (var pair in insertTargetsBySuffix)
        {
            await shardTableProvisioner.EnsureShardTableAsync(BusinessTaskLogicalTable, pair.Key, ct);
            using var scope = TableSuffixScope.Use(pair.Key);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            db.BusinessTasks.AddRange(pair.Value);
            await db.SaveChangesAsync(ct);
        }

        return uniqueEntitiesByKey.Count;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        entity.RefreshQueryFields();
        var loaded = await GetRequiredByIdAsync(entity.Id, entity.CreatedTimeLocal, ct);
        using var scope = TableSuffixScope.Use(loaded.Suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.BusinessTasks.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindPendingFeedbackAsync(int maxCount, CancellationToken ct)
    {
        return await QueryTopAcrossShardsAsync(query => query
            .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Pending)
            .OrderBy(x => x.CreatedTimeLocal), maxCount, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindFailedFeedbackAsync(int maxCount, CancellationToken ct)
    {
        return await QueryTopAcrossShardsAsync(query => query
            .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Failed)
            .OrderBy(x => x.CreatedTimeLocal), maxCount, ct);
    }

    /// <inheritdoc/>
    public async Task<int> BulkMarkExceptionByWaveCodeAsync(
        string waveCode,
        BusinessTaskStatus targetStatus,
        string failureReasonPrefix,
        DateTime updatedTimeLocal,
        CancellationToken ct)
    {
        var normalizedWaveCode = NormalizeOptionalText(waveCode);
        if (string.IsNullOrWhiteSpace(normalizedWaveCode))
        {
            return 0;
        }

        var affectedRows = 0;
        foreach (var suffix in await ListShardSuffixesWithLegacyFallbackAsync(ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            affectedRows += await db.BusinessTasks
                .Where(x => x.NormalizedWaveCode == normalizedWaveCode
                    && x.Status != BusinessTaskStatus.Dropped
                    && x.Status != BusinessTaskStatus.Exception)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, targetStatus)
                    .SetProperty(x => x.IsException, targetStatus == BusinessTaskStatus.Exception)
                    .SetProperty(x => x.FailureReason, failureReasonPrefix)
                    .SetProperty(x => x.UpdatedTimeLocal, updatedTimeLocal),
                    ct);
        }

        return affectedRows;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        var normalizedWaveCode = NormalizeOptionalText(waveCode);
        if (string.IsNullOrWhiteSpace(normalizedWaveCode))
        {
            return Array.Empty<BusinessTaskEntity>();
        }

        return await QueryAcrossShardsAsync(query => query.Where(x => x.NormalizedWaveCode == normalizedWaveCode), ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindActiveByBarcodeAsync(string barcode, CancellationToken ct)
    {
        var normalizedBarcode = NormalizeOptionalText(barcode);
        if (string.IsNullOrWhiteSpace(normalizedBarcode))
        {
            return Array.Empty<BusinessTaskEntity>();
        }

        return await QueryAcrossShardsAsync(query => query
            .Where(x => x.NormalizedBarcode == normalizedBarcode
                && x.Status != BusinessTaskStatus.Dropped
                && x.Status != BusinessTaskStatus.Exception), ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<BusinessTaskEntity>();
        }

        var shardSuffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        return await QueryAcrossSpecifiedShardsAsync(query => query
            .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal), shardSuffixes, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskWaveAggregateRow>> AggregateWaveDashboardAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<BusinessTaskWaveAggregateRow>();
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var merged = new Dictionary<string, BusinessTaskWaveAggregateRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardRows = await db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal)
                .GroupBy(x => x.NormalizedWaveCode ?? EmptyWaveCode)
                .Select(group => new BusinessTaskWaveAggregateRow
                {
                    WaveCode = group.Key,
                    TotalCount = group.Count(),
                    UnsortedCount = group.Count(task => task.Status != BusinessTaskStatus.Dropped && task.Status != BusinessTaskStatus.FeedbackPending),
                    FullCaseTotalCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase),
                    FullCaseUnsortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase && task.Status != BusinessTaskStatus.Dropped && task.Status != BusinessTaskStatus.FeedbackPending),
                    SplitTotalCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split),
                    SplitUnsortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split && task.Status != BusinessTaskStatus.Dropped && task.Status != BusinessTaskStatus.FeedbackPending),
                    RecognitionCount = group.Count(task => task.ScannedAtLocal != null),
                    RecirculatedCount = group.Count(task => task.IsRecirculated),
                    ExceptionCount = group.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception),
                    TotalVolumeMm3 = group.Sum(task => task.VolumeMm3 ?? 0M),
                    TotalWeightGram = group.Sum(task => task.WeightGram ?? 0M)
                })
                .ToListAsync(ct);

            MergeWaveAggregateRows(merged, shardRows);
        }

        return merged.Values
            .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ListWaveCodesByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<string>();
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var waveCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardCodes = await db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal)
                .Select(x => x.NormalizedWaveCode ?? EmptyWaveCode)
                .Distinct()
                .ToListAsync(ct);
            foreach (var code in shardCodes)
            {
                waveCodes.Add(code);
            }
        }

        return waveCodes
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskWaveOptionRow>> ListWaveOptionsByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<BusinessTaskWaveOptionRow>();
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var waveOptions = new Dictionary<string, WaveOptionCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardRows = await db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal)
                .GroupBy(x => new
                {
                    WaveCode = x.NormalizedWaveCode ?? EmptyWaveCode,
                    x.WaveRemark,
                })
                .Select(group => new ShardWaveOptionAggregateRow
                {
                    WaveCode = group.Key.WaveCode,
                    WaveRemark = group.Key.WaveRemark,
                    UpdatedTimeLocal = group.Max(x => x.UpdatedTimeLocal)
                })
                .ToListAsync(ct);
            foreach (var row in shardRows)
            {
                var normalizedRemark = NormalizeOptionalText(row.WaveRemark);
                if (!waveOptions.TryGetValue(row.WaveCode, out var existing))
                {
                    waveOptions[row.WaveCode] = new WaveOptionCandidate(normalizedRemark, row.UpdatedTimeLocal);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(normalizedRemark)
                    && (string.IsNullOrWhiteSpace(existing.WaveRemark) || row.UpdatedTimeLocal >= existing.UpdatedTimeLocal))
                {
                    waveOptions[row.WaveCode] = new WaveOptionCandidate(normalizedRemark, row.UpdatedTimeLocal);
                }
            }
        }

        return waveOptions
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new BusinessTaskWaveOptionRow
            {
                WaveCode = pair.Key,
                WaveRemark = pair.Value.WaveRemark
            })
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAndCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, string waveCode, CancellationToken ct)
    {
        var normalizedWaveCode = NormalizeOptionalText(waveCode);
        if (endTimeLocal <= startTimeLocal || string.IsNullOrWhiteSpace(normalizedWaveCode))
        {
            return Array.Empty<BusinessTaskEntity>();
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        return await QueryAcrossSpecifiedShardsAsync(query =>
        {
            var filtered = query.Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal);
            if (string.Equals(normalizedWaveCode, EmptyWaveCode, StringComparison.Ordinal))
            {
                return filtered.Where(x => x.NormalizedWaveCode == null);
            }

            return filtered.Where(x => x.NormalizedWaveCode == normalizedWaveCode);
        }, suffixes, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskWaveTaskStatsRow>> ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string waveCode,
        CancellationToken ct)
    {
        var normalizedWaveCode = NormalizeOptionalText(waveCode);
        if (endTimeLocal <= startTimeLocal || string.IsNullOrWhiteSpace(normalizedWaveCode))
        {
            return Array.Empty<BusinessTaskWaveTaskStatsRow>();
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var result = new List<BusinessTaskWaveTaskStatsRow>();
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var query = db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal);
            if (string.Equals(normalizedWaveCode, EmptyWaveCode, StringComparison.Ordinal))
            {
                query = query.Where(x => x.NormalizedWaveCode == null);
            }
            else
            {
                query = query.Where(x => x.NormalizedWaveCode == normalizedWaveCode);
            }

            var shardRows = await query
                .Select(x => new BusinessTaskWaveTaskStatsRow
                {
                    SourceType = x.SourceType,
                    WorkingArea = x.WorkingArea,
                    Status = x.Status,
                    ResolvedDockCode = x.ResolvedDockCode,
                    IsException = x.IsException,
                    WaveRemark = x.WaveRemark,
                    UpdatedTimeLocal = x.UpdatedTimeLocal
                })
                .ToListAsync(ct);
            if (shardRows.Count > 0)
            {
                result.AddRange(shardRows);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskDockAggregateRow>> AggregateDockDashboardAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? waveCode,
        string? dockCode,
        CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<BusinessTaskDockAggregateRow>();
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var merged = new Dictionary<string, BusinessTaskDockAggregateRow>(StringComparer.OrdinalIgnoreCase);
        var normalizedWaveCode = NormalizeOptionalText(waveCode);
        var normalizedDockCode = NormalizeOptionalText(dockCode);
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var query = db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal);
            if (!string.IsNullOrWhiteSpace(normalizedWaveCode))
            {
                if (string.Equals(normalizedWaveCode, EmptyWaveCode, StringComparison.Ordinal))
                {
                    query = query.Where(x => x.NormalizedWaveCode == null);
                }
                else
                {
                    query = query.Where(x => x.NormalizedWaveCode == normalizedWaveCode);
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedDockCode))
            {
                if (string.Equals(normalizedDockCode, EmptyDockCode, StringComparison.Ordinal))
                {
                    query = query.Where(x => x.ResolvedDockCode == EmptyDockCode);
                }
                else
                {
                    query = query.Where(x => x.ResolvedDockCode == normalizedDockCode);
                }
            }

            var shardRows = await query
                .GroupBy(x => x.ResolvedDockCode)
                .Select(group => new BusinessTaskDockAggregateRow
                {
                    DockCode = group.Key,
                    TotalCount = group.Count(),
                    SortedCount = group.Count(task => task.Status == BusinessTaskStatus.Dropped || task.Status == BusinessTaskStatus.FeedbackPending),
                    SplitUnsortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split && task.Status != BusinessTaskStatus.Dropped && task.Status != BusinessTaskStatus.FeedbackPending),
                    FullCaseUnsortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase && task.Status != BusinessTaskStatus.Dropped && task.Status != BusinessTaskStatus.FeedbackPending),
                    SplitTotalCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split),
                    FullCaseTotalCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase),
                    SplitSortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split && (task.Status == BusinessTaskStatus.Dropped || task.Status == BusinessTaskStatus.FeedbackPending)),
                    FullCaseSortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase && (task.Status == BusinessTaskStatus.Dropped || task.Status == BusinessTaskStatus.FeedbackPending)),
                    RecirculatedCount = group.Count(task => task.IsRecirculated),
                    ExceptionCount = group.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception)
                })
                .ToListAsync(ct);

            MergeDockAggregateRows(merged, shardRows);
        }

        return merged.Values
            .OrderBy(row => row.DockCode, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<int> CountByQueryConditionsAsync(BusinessTaskSearchFilter filter, CancellationToken ct)
    {
        if (filter.EndTimeLocal <= filter.StartTimeLocal)
        {
            return 0;
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(filter.StartTimeLocal, filter.EndTimeLocal, ct);
        var totalCount = 0;
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var query = BuildQueryBySearchFilter(db.BusinessTasks.AsNoTracking(), filter);
            totalCount += await query.CountAsync(ct);
        }

        return totalCount;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> QueryByQueryConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct)
    {
        var pageResult = await QueryPageWithTotalCountByConditionsAsync(filter, skip, take, ct);
        return pageResult.Items;
    }

    /// <inheritdoc/>
    public async Task<(int TotalCount, IReadOnlyList<BusinessTaskEntity> Items)> QueryPageWithTotalCountByConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct)
    {
        if (filter.EndTimeLocal <= filter.StartTimeLocal || take <= 0)
        {
            return (0, Array.Empty<BusinessTaskEntity>());
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(filter.StartTimeLocal, filter.EndTimeLocal, ct);
        var rows = new List<BusinessTaskEntity>(take);
        var remainingSkip = skip < 0 ? 0 : skip;
        var remainingTake = take;
        var totalCount = 0;
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var baseQuery = BuildQueryBySearchFilter(db.BusinessTasks.AsNoTracking(), filter);
            var shardCount = await baseQuery.CountAsync(ct);
            if (shardCount <= 0)
            {
                continue;
            }

            totalCount += shardCount;
            if (remainingTake <= 0)
            {
                continue;
            }

            if (remainingSkip >= shardCount)
            {
                remainingSkip -= shardCount;
                continue;
            }

            var query = baseQuery
                .OrderByDescending(task => task.CreatedTimeLocal)
                .ThenByDescending(task => task.Id);
            var shardRows = await query
                .Skip(remainingSkip)
                .Take(remainingTake)
                .ToListAsync(ct);
            rows.AddRange(shardRows);
            remainingTake -= shardRows.Count;
            remainingSkip = 0;
        }

        return (totalCount, rows);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> QueryByCursorConditionsAsync(
        BusinessTaskSearchFilter filter,
        DateTime? lastCreatedTimeLocal,
        long? lastId,
        int take,
        CancellationToken ct)
    {
        if (filter.EndTimeLocal <= filter.StartTimeLocal || take <= 0)
        {
            return Array.Empty<BusinessTaskEntity>();
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(filter.StartTimeLocal, filter.EndTimeLocal, ct);
        var rows = new List<BusinessTaskEntity>(take);
        var remainingTake = take;
        foreach (var suffix in suffixes)
        {
            if (remainingTake <= 0)
            {
                break;
            }

            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var query = BuildQueryBySearchFilter(db.BusinessTasks.AsNoTracking(), filter);
            if (lastCreatedTimeLocal.HasValue && lastId.HasValue)
            {
                var cursorCreatedTimeLocal = lastCreatedTimeLocal.Value;
                var cursorId = lastId.Value;
                query = query.Where(task =>
                    task.CreatedTimeLocal < cursorCreatedTimeLocal
                    || (task.CreatedTimeLocal == cursorCreatedTimeLocal && task.Id < cursorId));
            }

            var shardRows = await query
                .OrderByDescending(task => task.CreatedTimeLocal)
                .ThenByDescending(task => task.Id)
                .Take(remainingTake)
                .ToListAsync(ct);
            rows.AddRange(shardRows);
            remainingTake -= shardRows.Count;
        }

        return rows;
    }

    /// <summary>
    /// 在全部分片中查询首条记录。
    /// </summary>
    /// <param name="queryBuilder">查询构造函数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>首条记录；不存在时返回空。</returns>
    private async Task<BusinessTaskEntity?> FindFirstAcrossShardsAsync(
        Func<IQueryable<BusinessTaskEntity>, IQueryable<BusinessTaskEntity>> queryBuilder,
        CancellationToken ct)
    {
        foreach (var suffix in await ListShardSuffixesWithLegacyFallbackAsync(ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var entity = await queryBuilder(db.BusinessTasks.AsNoTracking()).FirstOrDefaultAsync(ct);
            if (entity is not null)
            {
                return entity;
            }
        }

        return null;
    }

    /// <summary>
    /// 在全部分片中查询数据并按创建时间升序返回。
    /// </summary>
    /// <param name="queryBuilder">查询构造函数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>聚合结果。</returns>
    private async Task<IReadOnlyList<BusinessTaskEntity>> QueryAcrossShardsAsync(
        Func<IQueryable<BusinessTaskEntity>, IQueryable<BusinessTaskEntity>> queryBuilder,
        CancellationToken ct)
    {
        var suffixes = await ListShardSuffixesWithLegacyFallbackAsync(ct);
        return await QueryAcrossSpecifiedShardsAsync(queryBuilder, suffixes, ct);
    }

    /// <summary>
    /// 在指定分片集合中查询数据并按创建时间升序返回。
    /// </summary>
    /// <param name="queryBuilder">查询构造函数。</param>
    /// <param name="shardSuffixes">需要查询的分片后缀集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>聚合结果。</returns>
    private async Task<IReadOnlyList<BusinessTaskEntity>> QueryAcrossSpecifiedShardsAsync(
        Func<IQueryable<BusinessTaskEntity>, IQueryable<BusinessTaskEntity>> queryBuilder,
        IReadOnlyList<string> shardSuffixes,
        CancellationToken ct)
    {
        if (shardSuffixes.Count == 0)
        {
            return Array.Empty<BusinessTaskEntity>();
        }

        var result = new List<BusinessTaskEntity>();
        foreach (var suffix in shardSuffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardRows = await queryBuilder(db.BusinessTasks.AsNoTracking()).ToListAsync(ct);
            if (shardRows.Count > 0)
            {
                result.AddRange(shardRows);
            }
        }

        return result
            .OrderBy(x => x.CreatedTimeLocal)
            .ToList();
    }

    /// <summary>
    /// 按查询过滤条件构建可组合查询。
    /// </summary>
    /// <param name="query">基础查询。</param>
    /// <param name="filter">查询过滤条件。</param>
    /// <returns>过滤后的查询。</returns>
    private static IQueryable<BusinessTaskEntity> BuildQueryBySearchFilter(
        IQueryable<BusinessTaskEntity> query,
        BusinessTaskSearchFilter filter)
    {
        var normalizedWaveCode = NormalizeOptionalText(filter.WaveCode);
        var normalizedBarcode = NormalizeOptionalText(filter.Barcode);
        var normalizedDockCode = NormalizeOptionalText(filter.DockCode);
        var normalizedChuteCode = NormalizeOptionalText(filter.ChuteCode);

        query = query.Where(task => task.CreatedTimeLocal >= filter.StartTimeLocal && task.CreatedTimeLocal < filter.EndTimeLocal);
        if (!string.IsNullOrWhiteSpace(normalizedWaveCode))
        {
            if (string.Equals(normalizedWaveCode, EmptyWaveCode, StringComparison.Ordinal))
            {
                query = query.Where(task => task.NormalizedWaveCode == null);
            }
            else
            {
                query = query.Where(task => task.NormalizedWaveCode == normalizedWaveCode);
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedBarcode))
        {
            query = query.Where(task => task.NormalizedBarcode == normalizedBarcode);
        }

        if (!string.IsNullOrWhiteSpace(normalizedDockCode))
        {
            query = query.Where(task => task.ResolvedDockCode == normalizedDockCode);
        }

        if (!string.IsNullOrWhiteSpace(normalizedChuteCode))
        {
            query = query.Where(task =>
                task.TargetChuteCode == normalizedChuteCode
                || task.ActualChuteCode == normalizedChuteCode);
        }

        if (filter.OnlyException)
        {
            query = query.Where(task => task.IsException || task.Status == BusinessTaskStatus.Exception);
        }

        if (filter.OnlyRecirculation)
        {
            query = query.Where(task => task.IsRecirculated);
        }

        return query;
    }

    /// <summary>
    /// 归一化可选文本，空白文本转为 null，其余文本执行 Trim。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <returns>归一化后的文本。</returns>
    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 波次选项候选值。
    /// </summary>
    /// <param name="waveRemark">波次备注。</param>
    /// <param name="updatedTimeLocal">更新时间。</param>
    private readonly record struct WaveOptionCandidate(string? WaveRemark, DateTime UpdatedTimeLocal);

    /// <summary>
    /// 分片内波次选项聚合行。
    /// </summary>
    private sealed class ShardWaveOptionAggregateRow
    {
        /// <summary>
        /// 波次号。
        /// </summary>
        public string WaveCode { get; set; } = string.Empty;

        /// <summary>
        /// 波次备注。
        /// </summary>
        public string? WaveRemark { get; set; }

        /// <summary>
        /// 更新时间。
        /// </summary>
        public DateTime UpdatedTimeLocal { get; set; }
    }

    /// <summary>
    /// 合并投影字段，避免覆盖运行态字段。
    /// </summary>
    /// <param name="target">已存在实体。</param>
    /// <param name="incoming">新投影实体。</param>
    private static void MergeProjectionFields(BusinessTaskEntity target, BusinessTaskEntity incoming)
    {
        if (target.Status == BusinessTaskStatus.Created && target.ScannedAtLocal is null && string.IsNullOrWhiteSpace(target.Barcode))
        {
            target.Barcode = incoming.Barcode;
        }

        target.WaveCode = incoming.WaveCode;
        target.WaveRemark = incoming.WaveRemark;
        target.WorkingArea = incoming.WorkingArea;
        target.UpdatedTimeLocal = incoming.UpdatedTimeLocal;
    }

    /// <summary>
    /// 加载投影幂等键在各分片中的已存在实体映射。
    /// </summary>
    /// <param name="keys">待匹配的投影幂等键。</param>
    /// <param name="sourceTableCodes">来源表编码集合。</param>
    /// <param name="businessKeys">业务键集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已存在映射。</returns>
    private async Task<Dictionary<ProjectionKey, LoadedBusinessTask>> LoadProjectionExistingMapAsync(
        IEnumerable<ProjectionKey> keys,
        IReadOnlyList<string> sourceTableCodes,
        IReadOnlyList<string> businessKeys,
        CancellationToken ct)
    {
        var keySet = keys.ToHashSet();
        var existingByKey = new Dictionary<ProjectionKey, LoadedBusinessTask>();
        foreach (var suffix in await ListShardSuffixesWithLegacyFallbackAsync(ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardRows = await db.BusinessTasks
                .AsNoTracking()
                .Where(task => sourceTableCodes.Contains(task.SourceTableCode) && businessKeys.Contains(task.BusinessKey))
                .ToListAsync(ct);
            foreach (var row in shardRows)
            {
                var key = ProjectionKey.Create(row.SourceTableCode, row.BusinessKey);
                if (key is null || !keySet.Contains(key.Value) || existingByKey.ContainsKey(key.Value))
                {
                    continue;
                }

                existingByKey[key.Value] = new LoadedBusinessTask(suffix, row);
            }
        }

        return existingByKey;
    }

    /// <summary>
    /// 合并波次聚合行。
    /// </summary>
    /// <param name="target">聚合目标字典。</param>
    /// <param name="rows">待合并行。</param>
    private static void MergeWaveAggregateRows(
        Dictionary<string, BusinessTaskWaveAggregateRow> target,
        IReadOnlyList<BusinessTaskWaveAggregateRow> rows)
    {
        foreach (var row in rows)
        {
            if (!target.TryGetValue(row.WaveCode, out var merged))
            {
                merged = new BusinessTaskWaveAggregateRow
                {
                    WaveCode = row.WaveCode
                };
                target[row.WaveCode] = merged;
            }

            merged.TotalCount += row.TotalCount;
            merged.UnsortedCount += row.UnsortedCount;
            merged.FullCaseTotalCount += row.FullCaseTotalCount;
            merged.FullCaseUnsortedCount += row.FullCaseUnsortedCount;
            merged.SplitTotalCount += row.SplitTotalCount;
            merged.SplitUnsortedCount += row.SplitUnsortedCount;
            merged.RecognitionCount += row.RecognitionCount;
            merged.RecirculatedCount += row.RecirculatedCount;
            merged.ExceptionCount += row.ExceptionCount;
            merged.TotalVolumeMm3 += row.TotalVolumeMm3;
            merged.TotalWeightGram += row.TotalWeightGram;
        }
    }

    /// <summary>
    /// 合并码头聚合行。
    /// </summary>
    /// <param name="target">聚合目标字典。</param>
    /// <param name="rows">待合并行。</param>
    private static void MergeDockAggregateRows(
        Dictionary<string, BusinessTaskDockAggregateRow> target,
        IReadOnlyList<BusinessTaskDockAggregateRow> rows)
    {
        foreach (var row in rows)
        {
            if (!target.TryGetValue(row.DockCode, out var merged))
            {
                merged = new BusinessTaskDockAggregateRow
                {
                    DockCode = row.DockCode
                };
                target[row.DockCode] = merged;
            }

            merged.TotalCount += row.TotalCount;
            merged.SortedCount += row.SortedCount;
            merged.SplitUnsortedCount += row.SplitUnsortedCount;
            merged.FullCaseUnsortedCount += row.FullCaseUnsortedCount;
            merged.SplitTotalCount += row.SplitTotalCount;
            merged.FullCaseTotalCount += row.FullCaseTotalCount;
            merged.SplitSortedCount += row.SplitSortedCount;
            merged.FullCaseSortedCount += row.FullCaseSortedCount;
            merged.RecirculatedCount += row.RecirculatedCount;
            merged.ExceptionCount += row.ExceptionCount;
        }
    }


    /// <summary>
    /// 在全部分片查询并截断返回数量。
    /// </summary>
    /// <param name="queryBuilder">查询构造函数。</param>
    /// <param name="maxCount">最大返回行数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>聚合后的前 N 行。</returns>
    private async Task<IReadOnlyList<BusinessTaskEntity>> QueryTopAcrossShardsAsync(
        Func<IQueryable<BusinessTaskEntity>, IQueryable<BusinessTaskEntity>> queryBuilder,
        int maxCount,
        CancellationToken ct)
    {
        var result = new List<BusinessTaskEntity>(maxCount);
        var suffixes = await ListShardSuffixesWithLegacyFallbackAsync(ct);
        if (suffixes.Contains(string.Empty, StringComparer.Ordinal))
        {
            var remainingCount = maxCount - result.Count;
            if (remainingCount > 0)
            {
                using var legacyScope = TableSuffixScope.Use(string.Empty);
                await using var legacyDb = await contextFactory.CreateDbContextAsync(ct);
                var legacyRows = await queryBuilder(legacyDb.BusinessTasks.AsNoTracking())
                    .Take(remainingCount)
                    .ToListAsync(ct);
                result.AddRange(legacyRows);
            }
        }

        for (var i = suffixes.Count - 1; i >= 0; i--)
        {
            var suffix = suffixes[i];
            if (string.IsNullOrEmpty(suffix))
            {
                continue;
            }

            var remainingCount = maxCount - result.Count;
            if (remainingCount <= 0)
            {
                break;
            }

            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardRows = await queryBuilder(db.BusinessTasks.AsNoTracking())
                .Take(remainingCount)
                .ToListAsync(ct);
            result.AddRange(shardRows);
        }

        return result
            .OrderBy(x => x.CreatedTimeLocal)
            .Take(maxCount)
            .ToList();
    }

    /// <summary>
    /// 通过 Id 定位必须存在的业务任务所在分片。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="createdTimeLocal">任务创建本地时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中的分片后缀与实体。</returns>
    /// <exception cref="InvalidOperationException">任务不存在时抛出。</exception>
    private async Task<LoadedBusinessTask> GetRequiredByIdAsync(long id, DateTime createdTimeLocal, CancellationToken ct)
    {
        if (createdTimeLocal != DateTime.MinValue)
        {
            var preferredSuffix = shardSuffixResolver.ResolveLocal(createdTimeLocal);
            var preferred = await TryFindByIdInSuffixAsync(id, preferredSuffix, ct);
            if (preferred is not null)
            {
                return preferred.Value;
            }
        }

        foreach (var suffix in await ListShardSuffixesWithLegacyFallbackAsync(ct))
        {
            var loaded = await TryFindByIdInSuffixAsync(id, suffix, ct);
            if (loaded is not null)
            {
                return loaded.Value;
            }
        }

        throw new InvalidOperationException($"未找到业务任务：{id}");
    }

    /// <summary>
    /// 在指定分片尝试按 Id 查询任务。
    /// </summary>
    /// <param name="id">任务主键。</param>
    /// <param name="suffix">分片后缀。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中结果。</returns>
    private async Task<LoadedBusinessTask?> TryFindByIdInSuffixAsync(long id, string suffix, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var entity = await db.BusinessTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? null : new LoadedBusinessTask(suffix, entity);
    }

    /// <summary>
    /// 列出现有分片后缀，并附加空后缀用于兼容历史固定表。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>分片后缀集合。</returns>
    private async Task<IReadOnlyList<string>> ListShardSuffixesWithLegacyFallbackAsync(CancellationToken ct)
    {
        var tables = await shardTableResolver.ListPhysicalTablesAsync(BusinessTaskLogicalTable, ct);
        var suffixes = tables
            .Select(table => table[BusinessTaskLogicalTable.Length..])
            .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(suffix => suffix, StringComparer.Ordinal)
            .ToList();
        if (_shardingOptions.EnableLegacyBaseTableReadFallback)
        {
            // 兼容历史固定表 business_tasks（无后缀），迁移窗口内保留读取能力。
            suffixes.Add(string.Empty);
        }

        return suffixes;
    }

    /// <summary>
    /// 根据创建时间范围计算需要命中的月份分片，并保留历史固定表兜底读取能力。
    /// </summary>
    /// <param name="startTimeLocal">开始时间（本地时间，含边界）。</param>
    /// <param name="endTimeLocal">结束时间（本地时间，不含边界）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中的分片后缀集合。</returns>
    private async Task<IReadOnlyList<string>> ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        var availableSuffixes = await ListShardSuffixesWithLegacyFallbackAsync(ct);
        if (availableSuffixes.Count == 0)
        {
            return Array.Empty<string>();
        }

        var targetSuffixes = new HashSet<string>(StringComparer.Ordinal);
        var currentMonth = new DateTime(startTimeLocal.Year, startTimeLocal.Month, 1, 0, 0, 0);
        var endInclusiveTime = endTimeLocal.AddTicks(-1);
        var endBoundaryMonth = new DateTime(endInclusiveTime.Year, endInclusiveTime.Month, 1, 0, 0, 0);

        while (currentMonth <= endBoundaryMonth)
        {
            targetSuffixes.Add(shardSuffixResolver.ResolveLocal(currentMonth));
            currentMonth = currentMonth.AddMonths(1);
        }

        // 容量预留：命中月份分片数量 + 可选历史固定表空后缀（用于兼容未分片的历史遗留数据）。
        var estimatedSuffixCount = targetSuffixes.Count + (_shardingOptions.EnableLegacyBaseTableReadFallback ? 1 : 0);
        var matchedSuffixes = new List<string>(estimatedSuffixCount);
        foreach (var suffix in availableSuffixes)
        {
            if (targetSuffixes.Contains(suffix))
            {
                matchedSuffixes.Add(suffix);
                continue;
            }

            if (_shardingOptions.EnableLegacyBaseTableReadFallback && string.IsNullOrEmpty(suffix))
            {
                matchedSuffixes.Add(suffix);
            }
        }

        return matchedSuffixes;
    }

    /// <summary>
    /// 业务任务分片查询结果。
    /// </summary>
    /// <param name="Suffix">分片后缀。</param>
    /// <param name="Entity">任务实体。</param>
    private readonly record struct LoadedBusinessTask(string Suffix, BusinessTaskEntity Entity);

    /// <summary>
    /// 投影更新目标。
    /// </summary>
    /// <param name="Id">目标实体主键。</param>
    /// <param name="Incoming">投影输入实体。</param>
    private readonly record struct ProjectionUpdateTarget(long Id, BusinessTaskEntity Incoming);

    /// <summary>
    /// 投影幂等键。
    /// </summary>
    /// <param name="SourceTableCode">来源表编码。</param>
    /// <param name="BusinessKey">业务键。</param>
    private readonly record struct ProjectionKey(string SourceTableCode, string BusinessKey)
    {
        /// <summary>
        /// 创建投影幂等键。
        /// </summary>
        /// <param name="sourceTableCode">来源表编码。</param>
        /// <param name="businessKey">业务键。</param>
        /// <returns>投影幂等键；任一输入为空白时返回 null。</returns>
        public static ProjectionKey? Create(string sourceTableCode, string businessKey)
        {
            var normalizedSourceTableCode = NormalizeOptionalText(sourceTableCode);
            var normalizedBusinessKey = NormalizeOptionalText(businessKey);
            if (string.IsNullOrWhiteSpace(normalizedSourceTableCode) || string.IsNullOrWhiteSpace(normalizedBusinessKey))
            {
                return null;
            }

            return new ProjectionKey(normalizedSourceTableCode, normalizedBusinessKey);
        }
    }
}
