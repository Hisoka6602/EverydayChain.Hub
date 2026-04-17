using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 业务任务仓储 EF Core 实现，按月写入与查询 <c>business_tasks_{yyyyMM}</c> 分表。
/// </summary>
public class BusinessTaskRepository(
    IDbContextFactory<HubDbContext> contextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IShardTableResolver shardTableResolver) : IBusinessTaskRepository
{
    /// <summary>业务任务逻辑表名。</summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <inheritdoc/>
    public async Task<BusinessTaskEntity?> FindByBarcodeAsync(string barcode, CancellationToken ct)
    {
        return await FindFirstAcrossShardsAsync(query => query
            .Where(x => x.Barcode == barcode)
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
    public async Task<BusinessTaskEntity?> FindByIdAsync(long id, CancellationToken ct)
    {
        return await FindFirstAcrossShardsAsync(query => query.Where(x => x.Id == id), ct);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        var suffix = shardSuffixResolver.ResolveLocal(entity.CreatedTimeLocal);
        await shardTableProvisioner.EnsureShardTableAsync(BusinessTaskLogicalTable, suffix, ct);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.BusinessTasks.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
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
        var affectedRows = 0;
        foreach (var suffix in await ListShardSuffixesWithLegacyFallbackAsync(ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            affectedRows += await db.BusinessTasks
                .Where(x => x.WaveCode == waveCode
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
        return await QueryAcrossShardsAsync(query => query.Where(x => x.WaveCode == waveCode), ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindActiveByBarcodeAsync(string barcode, CancellationToken ct)
    {
        return await QueryAcrossShardsAsync(query => query
            .Where(x => x.Barcode == barcode
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
        // 兼容历史固定表 business_tasks（无后缀），迁移窗口内保留读取能力。
        suffixes.Add(string.Empty);
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

        // 容量预留：命中月份分片数量 + 1 个历史固定表空后缀（用于兼容未分片的历史遗留数据）。
        var estimatedSuffixCount = targetSuffixes.Count + 1;
        var matchedSuffixes = new List<string>(estimatedSuffixCount);
        foreach (var suffix in availableSuffixes)
        {
            if (string.IsNullOrEmpty(suffix) || targetSuffixes.Contains(suffix))
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
}
