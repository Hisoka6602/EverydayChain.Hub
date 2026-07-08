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
using System.Linq.Expressions;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义 BusinessTaskRepository 类型。
/// </summary>
public class BusinessTaskRepository(
    IDbContextFactory<HubDbContext> contextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IShardTableResolver shardTableResolver,
    IOptions<ShardingOptions> shardingOptions,
    IOptions<DashboardSnapshotOptions> dashboardSnapshotOptions) : IBusinessTaskRepository
{
    /// <summary>
    /// 存储 BusinessTaskLogicalTable 字段。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";
    /// <summary>
    /// 存储 EmptyWaveCode 字段。
    /// </summary>
    private const string EmptyWaveCode = "未分波次";
    /// <summary>
    /// 存储 EmptyDockCode 字段。
    /// </summary>
    private const string EmptyDockCode = "未分配码头";
    /// <summary>
    /// 获取或设置 task。
    /// </summary>
    private static readonly Expression<Func<BusinessTaskEntity, bool>> RecirculationByResolvedDockCodeExpression = task =>
        task.ResolvedDockCode != string.Empty
        && !EF.Functions.Like(task.ResolvedDockCode, "%[^0-9]%")
        && Convert.ToInt32(task.ResolvedDockCode) > 7;
    /// <summary>
    /// 存储按创建时间升序比较业务任务的比较器。
    /// </summary>
    private static readonly IComparer<BusinessTaskEntity> CreatedTimeAscendingComparer = Comparer<BusinessTaskEntity>.Create((left, right) =>
    {
        // 步骤：先比较创建时间，再比较主键，保证排序稳定。
        var createdTimeComparison = left.CreatedTimeLocal.CompareTo(right.CreatedTimeLocal);
        if (createdTimeComparison != 0)
        {
            return createdTimeComparison;
        }

        return left.Id.CompareTo(right.Id);
    });

    /// <summary>
    /// 存储 _shardingOptions 字段。
    /// </summary>
    private readonly ShardingOptions _shardingOptions = shardingOptions.Value;
    /// <summary>
    /// 存储 _dashboardSnapshotOptions 字段。
    /// </summary>
    private readonly DashboardSnapshotOptions _dashboardSnapshotOptions = dashboardSnapshotOptions.Value;

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

    public async Task<BusinessTaskEntity?> FindByTaskCodeAsync(string taskCode, CancellationToken ct)
    {
        var normalizedTaskCode = NormalizeOptionalText(taskCode);
        if (string.IsNullOrWhiteSpace(normalizedTaskCode))
        {
            return null;
        }

        return await FindFirstAcrossShardsAsync(query => query
            .Where(x => x.TaskCode == normalizedTaskCode)
            .OrderByDescending(x => x.CreatedTimeLocal), ct);
    }

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

    public async Task<BusinessTaskEntity?> FindByIdAsync(long id, CancellationToken ct)
    {
        return await FindFirstAcrossShardsAsync(query => query.Where(x => x.Id == id), ct);
    }

    public async Task<IReadOnlyDictionary<long, BusinessTaskEntity>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<long, BusinessTaskEntity>();
        }

        var idSet = ids.Where(id => id > 0).Distinct().ToArray();
        if (idSet.Length == 0)
        {
            return new Dictionary<long, BusinessTaskEntity>();
        }

        var result = new Dictionary<long, BusinessTaskEntity>();
        foreach (var suffix in await ListShardSuffixesWithLegacyFallbackAsync(ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardRows = await db.BusinessTasks
                .AsNoTracking()
                .Where(task => idSet.Contains(task.Id))
                .ToListAsync(ct);
            foreach (var row in shardRows)
            {
                result.TryAdd(row.Id, row);
            }
        }

        return result;
    }

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

    public async Task UpsertProjectionAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        /// <summary>
        /// 执行 UpsertProjectionBatchAsync 方法。
        /// </summary>
        await UpsertProjectionBatchAsync([entity], ct);
    }

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

    public async Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        entity.RefreshQueryFields();
        var loaded = await GetRequiredByIdAsync(entity.Id, entity.CreatedTimeLocal, ct);
        using var scope = TableSuffixScope.Use(loaded.Suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.BusinessTasks.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryMarkScannedAsync(long taskId, DateTime createdTimeLocal, BusinessTaskScanUpdateCommand command, CancellationToken ct)
    {
        var suffix = shardSuffixResolver.ResolveLocal(createdTimeLocal);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var affectedRows = await db.BusinessTasks
            .Where(task => task.Id == taskId)
            .Where(task =>
                task.Status == BusinessTaskStatus.Created
                || task.Status == BusinessTaskStatus.Scanned
                || task.Status == BusinessTaskStatus.Dropped)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(task => task.Status, BusinessTaskStatus.Scanned)
                .SetProperty(task => task.ScannedAtLocal, command.ScanTimeLocal)
                .SetProperty(task => task.DeviceCode, task => command.DeviceCode ?? task.DeviceCode)
                .SetProperty(task => task.TraceId, task => command.TraceId ?? task.TraceId)
                .SetProperty(task => task.Barcode, task => string.IsNullOrWhiteSpace(task.Barcode) ? command.Barcode : task.Barcode)
                .SetProperty(task => task.LengthMm, task => command.LengthMm ?? task.LengthMm)
                .SetProperty(task => task.WidthMm, task => command.WidthMm ?? task.WidthMm)
                .SetProperty(task => task.HeightMm, task => command.HeightMm ?? task.HeightMm)
                .SetProperty(task => task.VolumeMm3, task => command.VolumeMm3 ?? task.VolumeMm3)
                .SetProperty(task => task.WeightGram, task => command.WeightGram ?? task.WeightGram)
                .SetProperty(task => task.TargetChuteCode, task => command.TargetChuteCode ?? task.TargetChuteCode)
                .SetProperty(task => task.ScanCount, task => task.ScanCount + 1)
                .SetProperty(task => task.DroppedAtLocal, task => task.Status == BusinessTaskStatus.Dropped ? null : task.DroppedAtLocal)
                .SetProperty(task => task.ActualChuteCode, task => task.Status == BusinessTaskStatus.Dropped ? null : task.ActualChuteCode)
                .SetProperty(task => task.FeedbackStatus, task => task.Status == BusinessTaskStatus.Dropped ? BusinessTaskFeedbackStatus.NotRequired : task.FeedbackStatus)
                .SetProperty(task => task.IsFeedbackReported, task => task.Status == BusinessTaskStatus.Dropped ? false : task.IsFeedbackReported)
                .SetProperty(task => task.FeedbackTimeLocal, task => task.Status == BusinessTaskStatus.Dropped ? null : task.FeedbackTimeLocal)
                .SetProperty(task => task.NormalizedBarcode, task => string.IsNullOrWhiteSpace(task.Barcode) ? command.Barcode : task.NormalizedBarcode)
                .SetProperty(task => task.ResolvedDockCode, task =>
                    !string.IsNullOrWhiteSpace(task.ActualChuteCode)
                        ? task.ActualChuteCode!
                        : (!string.IsNullOrWhiteSpace(command.TargetChuteCode)
                            ? command.TargetChuteCode
                            : (!string.IsNullOrWhiteSpace(task.TargetChuteCode) ? task.TargetChuteCode! : EmptyDockCode)))
                .SetProperty(task => task.UpdatedTimeLocal, command.UpdatedTimeLocal),
                ct);
        return affectedRows > 0;
    }

    public async Task<bool> IncrementScanRetryAsync(long taskId, DateTime createdTimeLocal, DateTime updatedTimeLocal, CancellationToken ct)
    {
        var suffix = shardSuffixResolver.ResolveLocal(createdTimeLocal);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var affectedRows = await db.BusinessTasks
            .Where(task => task.Id == taskId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(task => task.ScanRetryCount, task => task.ScanRetryCount + 1)
                .SetProperty(task => task.UpdatedTimeLocal, updatedTimeLocal),
                ct);
        return affectedRows > 0;
    }

    public async Task<IReadOnlyList<BusinessTaskEntity>> FindPendingFeedbackAsync(int maxCount, CancellationToken ct)
    {
        return await QueryTopAcrossShardsAsync(query => query
            .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Pending)
            .OrderBy(x => x.CreatedTimeLocal), maxCount, ct);
    }

    public async Task<IReadOnlyList<BusinessTaskEntity>> FindFailedFeedbackAsync(int maxCount, CancellationToken ct)
    {
        return await QueryTopAcrossShardsAsync(query => query
            .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Failed)
            .OrderBy(x => x.CreatedTimeLocal), maxCount, ct);
    }

    /// <summary>
    /// 执行 ClaimFeedbackBatchAsync 方法。
    /// </summary>
    public async Task<IReadOnlyList<BusinessTaskEntity>> ClaimFeedbackBatchAsync(
        BusinessTaskFeedbackStatus sourceStatus,
        int maxCount,
        DateTime claimedTimeLocal,
        TimeSpan staleAfter,
        CancellationToken ct)
    {
        // 步骤：执行 ClaimFeedbackBatchAsync 方法的核心处理流程。
        if (maxCount <= 0)
        {
            return [];
        }

        var staleCutoffLocal = claimedTimeLocal - staleAfter;
        var candidates = await QueryTopAcrossShardsAsync(query => query
            .Where(task =>
                task.FeedbackStatus == sourceStatus
                || (task.FeedbackStatus == BusinessTaskFeedbackStatus.Processing && task.UpdatedTimeLocal < staleCutoffLocal))
            .OrderBy(task => task.CreatedTimeLocal), maxCount * 4, ct);
        var claimed = new List<BusinessTaskEntity>(maxCount);
        foreach (var candidate in candidates)
        {
            if (claimed.Count >= maxCount)
            {
                break;
            }

            if (await TryClaimFeedbackRowAsync(candidate.Id, candidate.CreatedTimeLocal, sourceStatus, staleCutoffLocal, claimedTimeLocal, ct))
            {
                candidate.FeedbackStatus = BusinessTaskFeedbackStatus.Processing;
                candidate.UpdatedTimeLocal = claimedTimeLocal;
                claimed.Add(candidate);
            }
        }

        return claimed;
    }

    /// <summary>
    /// 执行 ClaimFeedbackByTaskCodeAsync 方法。
    /// </summary>
    public async Task<BusinessTaskEntity?> ClaimFeedbackByTaskCodeAsync(
        string taskCode,
        DateTime claimedTimeLocal,
        TimeSpan staleAfter,
        CancellationToken ct)
    {
        // 步骤：执行 ClaimFeedbackByTaskCodeAsync 方法的核心处理流程。
        var candidate = await FindByTaskCodeAsync(taskCode, ct);
        if (candidate is null)
        {
            return null;
        }

        var staleCutoffLocal = claimedTimeLocal - staleAfter;
        if (!await TryClaimFeedbackRowAsync(candidate.Id, candidate.CreatedTimeLocal, BusinessTaskFeedbackStatus.Failed, staleCutoffLocal, claimedTimeLocal, ct))
        {
            return null;
        }

        candidate.FeedbackStatus = BusinessTaskFeedbackStatus.Processing;
        candidate.UpdatedTimeLocal = claimedTimeLocal;
        return candidate;
    }

    public async Task<int> CompleteClaimedFeedbackBatchAsync(IReadOnlyCollection<long> ids, DateTime completedTimeLocal, CancellationToken ct)
    {
        return await UpdateClaimedFeedbackBatchAsync(
            ids,
            completedTimeLocal,
            BusinessTaskFeedbackStatus.Completed,
            isFeedbackReported: true,
            setFeedbackTime: true,
            ct);
    }

    public async Task<int> FailClaimedFeedbackBatchAsync(IReadOnlyCollection<long> ids, DateTime failedTimeLocal, CancellationToken ct)
    {
        return await UpdateClaimedFeedbackBatchAsync(
            ids,
            failedTimeLocal,
            BusinessTaskFeedbackStatus.Failed,
            isFeedbackReported: false,
            setFeedbackTime: false,
            ct);
    }

    /// <summary>
    /// 执行 BulkMarkExceptionByWaveCodeAsync 方法。
    /// </summary>
    public async Task<int> BulkMarkExceptionByWaveCodeAsync(
        string waveCode,
        BusinessTaskStatus targetStatus,
        string failureReasonPrefix,
        DateTime updatedTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 BulkMarkExceptionByWaveCodeAsync 方法的核心处理流程。
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

    public async Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        var normalizedWaveCode = NormalizeOptionalText(waveCode);
        if (string.IsNullOrWhiteSpace(normalizedWaveCode))
        {
            return Array.Empty<BusinessTaskEntity>();
        }

        return await QueryAcrossShardsAsync(query => query.Where(x => x.NormalizedWaveCode == normalizedWaveCode), ct);
    }

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

    public async Task<BusinessTaskEntity?> FindLatestScannedWithWaveByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return null;
        }

        if (await IsAlignedSnapshotRangeCoveredAsync(DashboardSnapshotSource.CurrentWave, startTimeLocal, endTimeLocal, ct))
        {
            return await FindLatestScannedWithWaveFromSnapshotsAsync(startTimeLocal, endTimeLocal, ct);
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        BusinessTaskEntity? latestTask = null;
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var candidate = await db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal
                    && x.CreatedTimeLocal < endTimeLocal
                    && x.ScannedAtLocal != null
                    && x.NormalizedWaveCode != null)
                .OrderByDescending(x => x.ScannedAtLocal)
                .ThenByDescending(x => x.UpdatedTimeLocal)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(ct);
            if (candidate is null)
            {
                continue;
            }

            if (latestTask is null || IsLaterScannedTask(candidate, latestTask))
            {
                latestTask = candidate;
            }
        }

        return latestTask;
    }

    public async Task<IReadOnlyList<BusinessTaskWaveAggregateRow>> AggregateWaveDashboardAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<BusinessTaskWaveAggregateRow>();
        }

        if (await IsAlignedSnapshotRangeCoveredAsync(DashboardSnapshotSource.BusinessTask, startTimeLocal, endTimeLocal, ct))
        {
            return await AggregateWaveDashboardFromSnapshotsAsync(startTimeLocal, endTimeLocal, ct);
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var merged = new Dictionary<string, BusinessTaskWaveAggregateRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var filteredQuery = db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal);
            var shardRows = await filteredQuery
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
                    RecirculatedCount = 0,
                    ExceptionCount = group.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception),
                    TotalVolumeMm3 = group.Sum(task => task.VolumeMm3 ?? 0M),
                    TotalWeightGram = group.Sum(task => task.WeightGram ?? 0M),
                    EarliestCreatedTimeLocal = group.Min(task => task.CreatedTimeLocal)
                })
                .ToListAsync(ct);
            var recirculatedCounts = await filteredQuery
                .Where(RecirculationByResolvedDockCodeExpression)
                .GroupBy(x => x.NormalizedWaveCode ?? EmptyWaveCode)
                .Select(group => new { WaveCode = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.WaveCode, row => row.Count, StringComparer.OrdinalIgnoreCase, ct);
            foreach (var row in shardRows)
            {
                if (recirculatedCounts.TryGetValue(row.WaveCode, out var count))
                {
                    row.RecirculatedCount = count;
                }
            }

            MergeWaveAggregateRows(merged, shardRows);
        }

        return merged.Values
            .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<BusinessTaskFeedbackAggregate> AggregateFeedbackAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return new BusinessTaskFeedbackAggregate();
        }

        if (await IsAlignedSnapshotRangeCoveredAsync(DashboardSnapshotSource.BusinessTask, startTimeLocal, endTimeLocal, ct))
        {
            return await AggregateFeedbackFromSnapshotsAsync(startTimeLocal, endTimeLocal, ct);
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var aggregate = new BusinessTaskFeedbackAggregate();
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardAggregate = await db.BusinessTasks
                .AsNoTracking()
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .GroupBy(_ => 1)
                .Select(group => new BusinessTaskFeedbackAggregate
                {
                    RequiredFeedbackCount = group.Count(task => task.FeedbackStatus != BusinessTaskFeedbackStatus.NotRequired),
                    CompletedFeedbackCount = group.Count(task => task.FeedbackStatus == BusinessTaskFeedbackStatus.Completed)
                })
                .FirstOrDefaultAsync(ct);
            if (shardAggregate is null)
            {
                continue;
            }

            aggregate.RequiredFeedbackCount += shardAggregate.RequiredFeedbackCount;
            aggregate.CompletedFeedbackCount += shardAggregate.CompletedFeedbackCount;
        }

        return aggregate;
    }

    public async Task<IReadOnlyList<string>> ListWaveCodesByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<string>();
        }

        if (await IsAlignedSnapshotRangeCoveredAsync(DashboardSnapshotSource.BusinessTask, startTimeLocal, endTimeLocal, ct))
        {
            return await ListWaveCodesFromSnapshotsAsync(startTimeLocal, endTimeLocal, ct);
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

    public async Task<IReadOnlyList<BusinessTaskWaveOptionRow>> ListWaveOptionsByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<BusinessTaskWaveOptionRow>();
        }

        if (await IsAlignedSnapshotRangeCoveredAsync(DashboardSnapshotSource.BusinessTask, startTimeLocal, endTimeLocal, ct))
        {
            return await ListWaveOptionsFromSnapshotsAsync(startTimeLocal, endTimeLocal, ct);
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
                (
                    group.Key.WaveCode,
                    group.Key.WaveRemark,
                    group.Max(x => x.UpdatedTimeLocal)
                ))
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

    /// <summary>
    /// 执行 ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync 方法。
    /// </summary>
    public async Task<IReadOnlyList<BusinessTaskWaveTaskStatsRow>> ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string waveCode,
        CancellationToken ct)
    {
        // 步骤：执行 ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync 方法的核心处理流程。
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
                    TaskCode = x.TaskCode,
                    WaveCode = x.NormalizedWaveCode ?? EmptyWaveCode,
                    SourceType = x.SourceType,
                    WorkingArea = x.WorkingArea,
                    Status = x.Status,
                    ResolvedDockCode = x.ResolvedDockCode,
                    IsException = x.IsException,
                    WaveRemark = x.WaveRemark,
                    UpdatedTimeLocal = x.UpdatedTimeLocal
                })
                .ToListAsync(ct);
            result.AddRange(shardRows);
        }

        return result;
    }

    /// <summary>
    /// 执行 AggregateDockDashboardAsync 方法。
    /// </summary>
    public async Task<IReadOnlyList<BusinessTaskDockAggregateRow>> AggregateDockDashboardAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? waveCode,
        string? dockCode,
        CancellationToken ct)
    {
        // 步骤：执行 AggregateDockDashboardAsync 方法的核心处理流程。
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<BusinessTaskDockAggregateRow>();
        }

        if (await IsAlignedSnapshotRangeCoveredAsync(DashboardSnapshotSource.BusinessTask, startTimeLocal, endTimeLocal, ct))
        {
            return await AggregateDockDashboardFromSnapshotsAsync(startTimeLocal, endTimeLocal, waveCode, dockCode, ct);
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var merged = new Dictionary<string, BusinessTaskDockAggregateRow>(StringComparer.OrdinalIgnoreCase);
        var normalizedWaveCode = NormalizeOptionalText(waveCode);
        var normalizedDockCode = NormalizeOptionalText(dockCode);
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var filteredQuery = db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal);
            if (!string.IsNullOrWhiteSpace(normalizedWaveCode))
            {
                if (string.Equals(normalizedWaveCode, EmptyWaveCode, StringComparison.Ordinal))
                {
                    filteredQuery = filteredQuery.Where(x => x.NormalizedWaveCode == null);
                }
                else
                {
                    filteredQuery = filteredQuery.Where(x => x.NormalizedWaveCode == normalizedWaveCode);
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedDockCode))
            {
                if (string.Equals(normalizedDockCode, EmptyDockCode, StringComparison.Ordinal))
                {
                    filteredQuery = filteredQuery.Where(x => x.ResolvedDockCode == EmptyDockCode);
                }
                else
                {
                    filteredQuery = filteredQuery.Where(x => x.ResolvedDockCode == normalizedDockCode);
                }
            }

            var shardRows = await filteredQuery
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
                    RecirculatedCount = 0,
                    ExceptionCount = group.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception)
                })
                .ToListAsync(ct);
            var recirculatedCounts = await filteredQuery
                .Where(RecirculationByResolvedDockCodeExpression)
                .GroupBy(x => x.ResolvedDockCode)
                .Select(group => new { DockCode = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.DockCode, row => row.Count, StringComparer.OrdinalIgnoreCase, ct);
            foreach (var row in shardRows)
            {
                if (recirculatedCounts.TryGetValue(row.DockCode, out var count))
                {
                    row.RecirculatedCount = count;
                }
            }

            MergeDockAggregateRows(merged, shardRows);
        }

        return merged.Values
            .OrderBy(row => row.DockCode, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 执行 AggregateRecirculationSummaryAsync 方法。
    /// </summary>
    public async Task<IReadOnlyList<BusinessTaskRecirculationAggregateRow>> AggregateRecirculationSummaryAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? chuteCode,
        CancellationToken ct)
    {
        // 步骤：执行 AggregateRecirculationSummaryAsync 方法的核心处理流程。
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<BusinessTaskRecirculationAggregateRow>();
        }

        if (await IsAlignedSnapshotRangeCoveredAsync(DashboardSnapshotSource.BusinessTask, startTimeLocal, endTimeLocal, ct))
        {
            return await AggregateRecirculationSummaryFromSnapshotsAsync(startTimeLocal, endTimeLocal, chuteCode, ct);
        }

        var suffixes = await ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(startTimeLocal, endTimeLocal, ct);
        var normalizedChuteCode = NormalizeOptionalText(chuteCode);
        var merged = new Dictionary<string, BusinessTaskRecirculationAggregateRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var filteredQuery = db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal)
                .Where(RecirculationByResolvedDockCodeExpression);
            if (!string.IsNullOrWhiteSpace(normalizedChuteCode))
            {
                filteredQuery = filteredQuery.Where(x =>
                    (x.ActualChuteCode != null && x.ActualChuteCode != string.Empty
                        ? x.ActualChuteCode
                        : (x.TargetChuteCode != null && x.TargetChuteCode != string.Empty ? x.TargetChuteCode : x.ResolvedDockCode))
                    == normalizedChuteCode);
            }

            var shardRows = await filteredQuery
                .GroupBy(x => new
                {
                    ChuteCode = x.ActualChuteCode != null && x.ActualChuteCode != string.Empty
                        ? x.ActualChuteCode
                        : (x.TargetChuteCode != null && x.TargetChuteCode != string.Empty ? x.TargetChuteCode : x.ResolvedDockCode),
                    WaveCode = x.NormalizedWaveCode ?? EmptyWaveCode
                })
                .Select(group => new BusinessTaskRecirculationAggregateRow
                {
                    ChuteCode = group.Key.ChuteCode,
                    WaveCode = group.Key.WaveCode,
                    RecirculatedCount = group.Count()
                })
                .ToListAsync(ct);
            MergeRecirculationAggregateRows(merged, shardRows);
        }

        return merged.Values
            .OrderBy(row => row.ChuteCode, StringComparer.Ordinal)
            .ThenBy(row => row.WaveCode, StringComparer.Ordinal)
            .ToList();
    }

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

    /// <summary>
    /// 执行 QueryByQueryConditionsAsync 方法。
    /// </summary>
    public async Task<IReadOnlyList<BusinessTaskEntity>> QueryByQueryConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct)
    {
        // 步骤：执行 QueryByQueryConditionsAsync 方法的核心处理流程。
        var pageResult = await QueryPageWithTotalCountByConditionsAsync(filter, skip, take, ct);
        return pageResult.Items;
    }

    /// <summary>
    /// 执行 QueryPageWithTotalCountByConditionsAsync 方法。
    /// </summary>
    public async Task<(int TotalCount, IReadOnlyList<BusinessTaskEntity> Items)> QueryPageWithTotalCountByConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct)
    {
        // 步骤：执行 QueryPageWithTotalCountByConditionsAsync 方法的核心处理流程。
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

    /// <summary>
    /// 执行 QueryByCursorConditionsAsync 方法。
    /// </summary>
    public async Task<IReadOnlyList<BusinessTaskEntity>> QueryByCursorConditionsAsync(
        BusinessTaskSearchFilter filter,
        DateTime? lastCreatedTimeLocal,
        long? lastId,
        int take,
        CancellationToken ct)
    {
        // 步骤：执行 QueryByCursorConditionsAsync 方法的核心处理流程。
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
    /// 执行 FindFirstAcrossShardsAsync 方法。
    /// </summary>
    private async Task<BusinessTaskEntity?> FindFirstAcrossShardsAsync(
        Func<IQueryable<BusinessTaskEntity>, IQueryable<BusinessTaskEntity>> queryBuilder,
        CancellationToken ct)
    {
        // 步骤：执行 FindFirstAcrossShardsAsync 方法的核心处理流程。
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
    /// 执行 QueryAcrossShardsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<BusinessTaskEntity>> QueryAcrossShardsAsync(
        Func<IQueryable<BusinessTaskEntity>, IQueryable<BusinessTaskEntity>> queryBuilder,
        CancellationToken ct)
    {
        // 步骤：执行 QueryAcrossShardsAsync 方法的核心处理流程。
        var suffixes = await ListShardSuffixesWithLegacyFallbackAsync(ct);
        return await QueryAcrossSpecifiedShardsAsync(queryBuilder, suffixes, ct);
    }

    /// <summary>
    /// 执行 QueryAcrossSpecifiedShardsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<BusinessTaskEntity>> QueryAcrossSpecifiedShardsAsync(
        Func<IQueryable<BusinessTaskEntity>, IQueryable<BusinessTaskEntity>> queryBuilder,
        IReadOnlyList<string> shardSuffixes,
        CancellationToken ct)
    {
        // 步骤：执行 QueryAcrossSpecifiedShardsAsync 方法的核心处理流程。
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
    /// 执行 BuildQueryBySearchFilter 方法。
    /// </summary>
    private static IQueryable<BusinessTaskEntity> BuildQueryBySearchFilter(
        IQueryable<BusinessTaskEntity> query,
        BusinessTaskSearchFilter filter)
    {
        // 步骤：执行 BuildQueryBySearchFilter 方法的核心处理流程。
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
            query = query.Where(RecirculationByResolvedDockCodeExpression);
        }

        return query;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 定义 WaveOptionCandidate 类型。
    /// </summary>
    private readonly record struct WaveOptionCandidate(string? WaveRemark, DateTime UpdatedTimeLocal);

    /// <summary>
    /// 定义 ShardWaveOptionAggregateRow 类型。
    /// </summary>
    private readonly record struct ShardWaveOptionAggregateRow(
        string WaveCode,
        string? WaveRemark,
        DateTime UpdatedTimeLocal);

    /// <summary>
    /// 执行 IsAlignedSnapshotRangeCoveredAsync 方法。
    /// </summary>
    private async Task<bool> IsAlignedSnapshotRangeCoveredAsync(
        DashboardSnapshotSource source,
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 IsAlignedSnapshotRangeCoveredAsync 方法的核心处理流程。
        if (!_dashboardSnapshotOptions.Enabled || !_dashboardSnapshotOptions.PreferSnapshotQueries)
        {
            return false;
        }

        if (!IsMinuteAligned(startTimeLocal) || !IsMinuteAligned(endTimeLocal))
        {
            return false;
        }

        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var state = await db.DashboardSnapshotStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == source, ct);
        return state?.CoverageStartLocal <= startTimeLocal
            && state.CoverageEndLocal >= endTimeLocal;
    }

    /// <summary>
    /// 执行 AggregateWaveDashboardFromSnapshotsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<BusinessTaskWaveAggregateRow>> AggregateWaveDashboardFromSnapshotsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 AggregateWaveDashboardFromSnapshotsAsync 方法的核心处理流程。
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        return await db.DashboardTaskSnapshots
            .AsNoTracking()
            .Where(x => x.BucketStartLocal >= startTimeLocal && x.BucketStartLocal < endTimeLocal)
            .GroupBy(x => x.WaveCode)
            .Select(group => new BusinessTaskWaveAggregateRow
            {
                WaveCode = group.Key,
                TotalCount = group.Sum(x => x.TotalCount),
                UnsortedCount = group.Sum(x => x.Status != BusinessTaskStatus.Dropped && x.Status != BusinessTaskStatus.FeedbackPending ? x.TotalCount : 0),
                FullCaseTotalCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.FullCase ? x.TotalCount : 0),
                FullCaseUnsortedCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.FullCase && x.Status != BusinessTaskStatus.Dropped && x.Status != BusinessTaskStatus.FeedbackPending ? x.TotalCount : 0),
                SplitTotalCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.Split ? x.TotalCount : 0),
                SplitUnsortedCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.Split && x.Status != BusinessTaskStatus.Dropped && x.Status != BusinessTaskStatus.FeedbackPending ? x.TotalCount : 0),
                RecognitionCount = group.Sum(x => x.ScannedCount),
                RecirculatedCount = group.Sum(x => x.RecirculatedCount),
                ExceptionCount = group.Sum(x => x.ExceptionCount),
                TotalVolumeMm3 = group.Sum(x => x.TotalVolumeMm3),
                TotalWeightGram = group.Sum(x => x.TotalWeightGram),
                EarliestCreatedTimeLocal = group.Min(x => x.EarliestCreatedTimeLocal)
            })
            .OrderBy(x => x.WaveCode)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 执行 AggregateFeedbackFromSnapshotsAsync 方法。
    /// </summary>
    private async Task<BusinessTaskFeedbackAggregate> AggregateFeedbackFromSnapshotsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 AggregateFeedbackFromSnapshotsAsync 方法的核心处理流程。
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var aggregate = await db.DashboardTaskSnapshots
            .AsNoTracking()
            .Where(x => x.BucketStartLocal >= startTimeLocal && x.BucketStartLocal < endTimeLocal)
            .GroupBy(_ => 1)
            .Select(group => new BusinessTaskFeedbackAggregate
            {
                RequiredFeedbackCount = group.Sum(x => x.RequiredFeedbackCount),
                CompletedFeedbackCount = group.Sum(x => x.CompletedFeedbackCount)
            })
            .FirstOrDefaultAsync(ct);
        return aggregate ?? new BusinessTaskFeedbackAggregate();
    }

    /// <summary>
    /// 执行 ListWaveCodesFromSnapshotsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<string>> ListWaveCodesFromSnapshotsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 ListWaveCodesFromSnapshotsAsync 方法的核心处理流程。
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        return await db.DashboardTaskSnapshots
            .AsNoTracking()
            .Where(x => x.BucketStartLocal >= startTimeLocal && x.BucketStartLocal < endTimeLocal)
            .Select(x => x.WaveCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 执行 ListWaveOptionsFromSnapshotsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<BusinessTaskWaveOptionRow>> ListWaveOptionsFromSnapshotsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 ListWaveOptionsFromSnapshotsAsync 方法的核心处理流程。
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var rows = await db.DashboardTaskSnapshots
            .AsNoTracking()
            .Where(x => x.BucketStartLocal >= startTimeLocal && x.BucketStartLocal < endTimeLocal)
            .GroupBy(x => new { x.WaveCode, x.WaveRemark })
            .Select(group => new ShardWaveOptionAggregateRow(
                group.Key.WaveCode,
                group.Key.WaveRemark,
                group.Max(x => x.LatestUpdatedTimeLocal)))
            .ToListAsync(ct);

        var options = new Dictionary<string, WaveOptionCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var normalizedRemark = NormalizeOptionalText(row.WaveRemark);
            if (!options.TryGetValue(row.WaveCode, out var existing))
            {
                options[row.WaveCode] = new WaveOptionCandidate(normalizedRemark, row.UpdatedTimeLocal);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(normalizedRemark)
                && (string.IsNullOrWhiteSpace(existing.WaveRemark) || row.UpdatedTimeLocal >= existing.UpdatedTimeLocal))
            {
                options[row.WaveCode] = new WaveOptionCandidate(normalizedRemark, row.UpdatedTimeLocal);
            }
        }

        return options
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new BusinessTaskWaveOptionRow
            {
                WaveCode = pair.Key,
                WaveRemark = pair.Value.WaveRemark
            })
            .ToList();
    }

    /// <summary>
    /// 执行 AggregateDockDashboardFromSnapshotsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<BusinessTaskDockAggregateRow>> AggregateDockDashboardFromSnapshotsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? waveCode,
        string? dockCode,
        CancellationToken ct)
    {
        // 步骤：执行 AggregateDockDashboardFromSnapshotsAsync 方法的核心处理流程。
        var normalizedWaveCode = NormalizeOptionalText(waveCode);
        var normalizedDockCode = NormalizeOptionalText(dockCode);
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.DashboardTaskSnapshots
            .AsNoTracking()
            .Where(x => x.BucketStartLocal >= startTimeLocal && x.BucketStartLocal < endTimeLocal);
        if (!string.IsNullOrWhiteSpace(normalizedWaveCode))
        {
            query = query.Where(x => x.WaveCode == normalizedWaveCode);
        }

        if (!string.IsNullOrWhiteSpace(normalizedDockCode))
        {
            query = query.Where(x => x.ResolvedDockCode == normalizedDockCode);
        }

        return await query
            .GroupBy(x => x.ResolvedDockCode)
            .Select(group => new BusinessTaskDockAggregateRow
            {
                DockCode = group.Key,
                TotalCount = group.Sum(x => x.TotalCount),
                SortedCount = group.Sum(x => x.Status == BusinessTaskStatus.Dropped || x.Status == BusinessTaskStatus.FeedbackPending ? x.TotalCount : 0),
                SplitUnsortedCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.Split && x.Status != BusinessTaskStatus.Dropped && x.Status != BusinessTaskStatus.FeedbackPending ? x.TotalCount : 0),
                FullCaseUnsortedCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.FullCase && x.Status != BusinessTaskStatus.Dropped && x.Status != BusinessTaskStatus.FeedbackPending ? x.TotalCount : 0),
                SplitTotalCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.Split ? x.TotalCount : 0),
                FullCaseTotalCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.FullCase ? x.TotalCount : 0),
                SplitSortedCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.Split && (x.Status == BusinessTaskStatus.Dropped || x.Status == BusinessTaskStatus.FeedbackPending) ? x.TotalCount : 0),
                FullCaseSortedCount = group.Sum(x => x.SourceType == BusinessTaskSourceType.FullCase && (x.Status == BusinessTaskStatus.Dropped || x.Status == BusinessTaskStatus.FeedbackPending) ? x.TotalCount : 0),
                RecirculatedCount = group.Sum(x => x.RecirculatedCount),
                ExceptionCount = group.Sum(x => x.ExceptionCount)
            })
            .OrderBy(x => x.DockCode)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 执行 AggregateRecirculationSummaryFromSnapshotsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<BusinessTaskRecirculationAggregateRow>> AggregateRecirculationSummaryFromSnapshotsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? chuteCode,
        CancellationToken ct)
    {
        // 步骤：执行 AggregateRecirculationSummaryFromSnapshotsAsync 方法的核心处理流程。
        var normalizedChuteCode = NormalizeOptionalText(chuteCode);
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.DashboardTaskSnapshots
            .AsNoTracking()
            .Where(x => x.BucketStartLocal >= startTimeLocal && x.BucketStartLocal < endTimeLocal)
            .Where(x => x.RecirculatedCount > 0);
        if (!string.IsNullOrWhiteSpace(normalizedChuteCode))
        {
            query = query.Where(x => x.ResolvedDockCode == normalizedChuteCode);
        }

        return await query
            .GroupBy(x => new { x.ResolvedDockCode, x.WaveCode })
            .Select(group => new BusinessTaskRecirculationAggregateRow
            {
                ChuteCode = group.Key.ResolvedDockCode,
                WaveCode = group.Key.WaveCode,
                RecirculatedCount = group.Sum(x => x.RecirculatedCount)
            })
            .OrderBy(x => x.ChuteCode)
            .ThenBy(x => x.WaveCode)
            .ToListAsync(ct);
    }

    private static bool IsMinuteAligned(DateTime value)
    {
        return value.Second == 0
            && value.Millisecond == 0
            && value.Ticks % TimeSpan.TicksPerSecond == 0;
    }

    private static void MergeProjectionFields(BusinessTaskEntity target, BusinessTaskEntity incoming)
    {
        if (target.Status == BusinessTaskStatus.Created && target.ScannedAtLocal is null && string.IsNullOrWhiteSpace(target.Barcode))
        {
            target.Barcode = incoming.Barcode;
        }

        target.WaveCode = incoming.WaveCode;
        target.WaveRemark = incoming.WaveRemark;
        target.WorkingArea = incoming.WorkingArea;
        target.OrderId = incoming.OrderId;
        target.StoreId = incoming.StoreId;
        target.StoreName = incoming.StoreName;
        target.ProductCode = incoming.ProductCode;
        target.PickLocation = incoming.PickLocation;
        target.UpdatedTimeLocal = incoming.UpdatedTimeLocal;
    }

    /// <summary>
    /// 执行 LoadProjectionExistingMapAsync 方法。
    /// </summary>
    private async Task<Dictionary<ProjectionKey, LoadedBusinessTask>> LoadProjectionExistingMapAsync(
        IEnumerable<ProjectionKey> keys,
        IReadOnlyList<string> sourceTableCodes,
        IReadOnlyList<string> businessKeys,
        CancellationToken ct)
    {
        // 步骤：执行 LoadProjectionExistingMapAsync 方法的核心处理流程。
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
    /// 执行 MergeWaveAggregateRows 方法。
    /// </summary>
    private static void MergeWaveAggregateRows(
        Dictionary<string, BusinessTaskWaveAggregateRow> target,
        IReadOnlyList<BusinessTaskWaveAggregateRow> rows)
    {
        // 步骤：执行 MergeWaveAggregateRows 方法的核心处理流程。
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
            if (merged.EarliestCreatedTimeLocal == default
                || (row.EarliestCreatedTimeLocal != default && row.EarliestCreatedTimeLocal < merged.EarliestCreatedTimeLocal))
            {
                merged.EarliestCreatedTimeLocal = row.EarliestCreatedTimeLocal;
            }
        }
    }

    /// <summary>
    /// 执行 MergeDockAggregateRows 方法。
    /// </summary>
    private static void MergeDockAggregateRows(
        Dictionary<string, BusinessTaskDockAggregateRow> target,
        IReadOnlyList<BusinessTaskDockAggregateRow> rows)
    {
        // 步骤：执行 MergeDockAggregateRows 方法的核心处理流程。
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
    /// 执行 MergeRecirculationAggregateRows 方法。
    /// </summary>
    private static void MergeRecirculationAggregateRows(
        Dictionary<string, BusinessTaskRecirculationAggregateRow> target,
        IReadOnlyList<BusinessTaskRecirculationAggregateRow> rows)
    {
        // 步骤：执行 MergeRecirculationAggregateRows 方法的核心处理流程。
        foreach (var row in rows)
        {
            var key = $"{row.ChuteCode}||{row.WaveCode}";
            if (!target.TryGetValue(key, out var merged))
            {
                merged = new BusinessTaskRecirculationAggregateRow
                {
                    ChuteCode = row.ChuteCode,
                    WaveCode = row.WaveCode
                };
                target[key] = merged;
            }

            merged.RecirculatedCount += row.RecirculatedCount;
        }
    }

    private static bool IsLaterScannedTask(BusinessTaskEntity left, BusinessTaskEntity right)
    {
        var leftScanTime = left.ScannedAtLocal ?? DateTime.MinValue;
        var rightScanTime = right.ScannedAtLocal ?? DateTime.MinValue;
        if (leftScanTime != rightScanTime)
        {
            return leftScanTime > rightScanTime;
        }

        if (left.UpdatedTimeLocal != right.UpdatedTimeLocal)
        {
            return left.UpdatedTimeLocal > right.UpdatedTimeLocal;
        }

        return left.Id > right.Id;
    }

    /// <summary>
    /// 从当前波次快照表中读取最新已扫描波次。
    /// </summary>
    /// <param name="startTimeLocal">开始时间。</param>
    /// <param name="endTimeLocal">结束时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>最新已扫描且带波次的任务投影。</returns>
    private async Task<BusinessTaskEntity?> FindLatestScannedWithWaveFromSnapshotsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 FindLatestScannedWithWaveFromSnapshotsAsync 方法的核心处理流程。
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var snapshot = await db.DashboardCurrentWaveSnapshots
            .AsNoTracking()
            .Where(x => x.BucketStartLocal >= startTimeLocal && x.BucketStartLocal < endTimeLocal)
            .OrderByDescending(x => x.ScannedAtLocal)
            .ThenByDescending(x => x.BucketStartLocal)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
        if (snapshot is null)
        {
            return null;
        }

        return new BusinessTaskEntity
        {
            WaveCode = snapshot.WaveCode,
            WaveRemark = snapshot.WaveRemark,
            Barcode = snapshot.Barcode,
            ScannedAtLocal = snapshot.ScannedAtLocal,
            CreatedTimeLocal = snapshot.BucketStartLocal,
            UpdatedTimeLocal = snapshot.ScannedAtLocal
        };
    }


    /// <summary>
    /// 执行 QueryTopAcrossShardsAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<BusinessTaskEntity>> QueryTopAcrossShardsAsync(
        Func<IQueryable<BusinessTaskEntity>, IQueryable<BusinessTaskEntity>> queryBuilder,
        int maxCount,
        CancellationToken ct)
    {
        // 步骤：执行 QueryTopAcrossShardsAsync 方法的核心处理流程。
        if (maxCount <= 0)
        {
            return [];
        }

        var result = new List<BusinessTaskEntity>(maxCount);
        var suffixes = await ListShardSuffixesWithLegacyFallbackAsync(ct);
        if (suffixes.Contains(string.Empty, StringComparer.Ordinal))
        {
            using var legacyScope = TableSuffixScope.Use(string.Empty);
            await using var legacyDb = await contextFactory.CreateDbContextAsync(ct);
            var legacyRows = await queryBuilder(legacyDb.BusinessTasks.AsNoTracking())
                .Take(maxCount)
                .ToListAsync(ct);
            result.AddRange(legacyRows);
        }

        for (var i = suffixes.Count - 1; i >= 0; i--)
        {
            var suffix = suffixes[i];
            if (string.IsNullOrEmpty(suffix))
            {
                continue;
            }

            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardRows = await queryBuilder(db.BusinessTasks.AsNoTracking())
                .Take(maxCount)
                .ToListAsync(ct);
            result.AddRange(shardRows);
        }

        return result
            .OrderBy(x => x, CreatedTimeAscendingComparer)
            .Take(maxCount)
            .ToList();
    }

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
    /// 执行 TryClaimFeedbackRowAsync 方法。
    /// </summary>
    private async Task<bool> TryClaimFeedbackRowAsync(
        long taskId,
        DateTime createdTimeLocal,
        BusinessTaskFeedbackStatus sourceStatus,
        DateTime staleCutoffLocal,
        DateTime claimedTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 InvalidOperationException 方法的核心处理流程。
        var suffix = shardSuffixResolver.ResolveLocal(createdTimeLocal);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var affectedRows = await db.BusinessTasks
            .Where(task => task.Id == taskId)
            .Where(task =>
                task.FeedbackStatus == sourceStatus
                || (task.FeedbackStatus == BusinessTaskFeedbackStatus.Processing && task.UpdatedTimeLocal < staleCutoffLocal))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(task => task.FeedbackStatus, BusinessTaskFeedbackStatus.Processing)
                .SetProperty(task => task.UpdatedTimeLocal, claimedTimeLocal),
                ct);
        return affectedRows > 0;
    }

    /// <summary>
    /// 执行 UpdateClaimedFeedbackBatchAsync 方法。
    /// </summary>
    private async Task<int> UpdateClaimedFeedbackBatchAsync(
        IReadOnlyCollection<long> ids,
        DateTime updatedTimeLocal,
        BusinessTaskFeedbackStatus targetStatus,
        bool isFeedbackReported,
        bool setFeedbackTime,
        CancellationToken ct)
    {
        // 步骤：执行 SetProperty 方法的核心处理流程。
        if (ids.Count == 0)
        {
            return 0;
        }

        var idSet = ids.Where(id => id > 0).Distinct().ToArray();
        if (idSet.Length == 0)
        {
            return 0;
        }

        var affectedRows = 0;
        foreach (var suffix in await ListShardSuffixesWithLegacyFallbackAsync(ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            affectedRows += await db.BusinessTasks
                .Where(task => idSet.Contains(task.Id) && task.FeedbackStatus == BusinessTaskFeedbackStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(task => task.FeedbackStatus, targetStatus)
                    .SetProperty(task => task.IsFeedbackReported, isFeedbackReported)
                    .SetProperty(task => task.FeedbackTimeLocal, setFeedbackTime ? updatedTimeLocal : null)
                    .SetProperty(task => task.UpdatedTimeLocal, updatedTimeLocal),
                    ct);
        }

        return affectedRows;
    }

    private async Task<LoadedBusinessTask?> TryFindByIdInSuffixAsync(long id, string suffix, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var entity = await db.BusinessTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? null : new LoadedBusinessTask(suffix, entity);
    }

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
            suffixes.Add(string.Empty);
        }

        return suffixes;
    }

    /// <summary>
    /// 执行 ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<string>> ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 ListShardSuffixesByCreatedTimeRangeWithLegacyFallbackAsync 方法的核心处理流程。
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
    /// 定义 LoadedBusinessTask 类型。
    /// </summary>
    private readonly record struct LoadedBusinessTask(string Suffix, BusinessTaskEntity Entity);

    /// <summary>
    /// 定义 ProjectionUpdateTarget 类型。
    /// </summary>
    private readonly record struct ProjectionUpdateTarget(long Id, BusinessTaskEntity Incoming);

    /// <summary>
    /// 定义 ProjectionKey 类型。
    /// </summary>
    private readonly record struct ProjectionKey(string SourceTableCode, string BusinessKey)
    {
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


