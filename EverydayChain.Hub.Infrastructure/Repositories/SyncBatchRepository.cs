using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义 SyncBatchRepository 类型。
/// </summary>
public class SyncBatchRepository(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IShardTableResolver shardTableResolver,
    ILogger<SyncBatchRepository> logger) : ISyncBatchRepository
{
    /// <summary>
    /// 存储 SyncBatchLogicalTable 字段。
    /// </summary>
    private const string SyncBatchLogicalTable = "sync_batches";

    public async Task CreateBatchAsync(SyncBatch batch, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            ValidateBatchForCreate(batch);
            if (await TryGetBatchFromExistingShardsAsync(batch.BatchId, ct) is not null)
            {
                throw new InvalidOperationException($"批次已存在：{batch.BatchId}");
            }

            var suffix = ResolveBatchSuffix(batch.WindowEndLocal);
            await shardTableProvisioner.EnsureShardTableAsync(SyncBatchLogicalTable, suffix, ct);
            using var scope = TableSuffixScope.Use(suffix);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            dbContext.SyncBatches.Add(MapToEntity(batch, SyncBatchStatus.Pending));
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建同步批次失败。BatchId={BatchId}, TableCode={TableCode}", batch.BatchId, batch.TableCode);
            throw;
        }
    }

    public async Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var loaded = await GetRequiredBatchFromShardsAsync(batchId, ct);
            loaded.Entity.Status = SyncBatchStatus.InProgress;
            loaded.Entity.StartedTimeLocal = startedTimeLocal;
            if (!string.IsNullOrWhiteSpace(loaded.Entity.ErrorMessage))
            {
                logger.LogInformation("批次重试进入执行中，清理历史错误信息。BatchId={BatchId}", batchId);
            }
            loaded.Entity.ErrorMessage = null;
            using var scope = TableSuffixScope.Use(loaded.Suffix);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            dbContext.SyncBatches.Update(loaded.Entity);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "标记批次执行中失败。BatchId={BatchId}", batchId);
            throw;
        }
    }

    public async Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var loaded = await GetRequiredBatchFromShardsAsync(result.BatchId, ct);
            loaded.Entity.Status = SyncBatchStatus.Completed;
            loaded.Entity.CompletedTimeLocal = completedTimeLocal;
            loaded.Entity.ReadCount = result.ReadCount;
            loaded.Entity.InsertCount = result.InsertCount;
            loaded.Entity.UpdateCount = result.UpdateCount;
            loaded.Entity.DeleteCount = result.DeleteCount;
            loaded.Entity.SkipCount = result.SkipCount;
            loaded.Entity.ErrorMessage = null;
            using var scope = TableSuffixScope.Use(loaded.Suffix);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            dbContext.SyncBatches.Update(loaded.Entity);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "标记批次完成失败。BatchId={BatchId}", result.BatchId);
            throw;
        }
    }

    public async Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var loaded = await GetRequiredBatchFromShardsAsync(batchId, ct);
            loaded.Entity.Status = SyncBatchStatus.Failed;
            loaded.Entity.CompletedTimeLocal = failedTimeLocal;
            loaded.Entity.ErrorMessage = errorMessage;
            using var scope = TableSuffixScope.Use(loaded.Suffix);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            dbContext.SyncBatches.Update(loaded.Entity);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "标记批次失败状态失败。BatchId={BatchId}", batchId);
            throw;
        }
    }

    public async Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var maxCompletedTime = DateTime.MinValue;
            var latestBatchId = default(string);
            var suffixes = await ListExistingShardSuffixesAsync(ct);
            foreach (var suffix in suffixes)
            {
                using var scope = TableSuffixScope.Use(suffix);
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
                var candidate = await dbContext.SyncBatches
                    .AsNoTracking()
                    .Where(batch => batch.Status == SyncBatchStatus.Failed)
                    .Where(batch => batch.TableCode == tableCode)
                    .OrderByDescending(batch => batch.CompletedTimeLocal)
                    .FirstOrDefaultAsync(ct);
                if (candidate is null)
                {
                    continue;
                }

                var completedTime = candidate.CompletedTimeLocal ?? DateTime.MinValue;
                if (completedTime > maxCompletedTime)
                {
                    maxCompletedTime = completedTime;
                    latestBatchId = candidate.BatchId;
                }
            }

            return latestBatchId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查询最近失败批次失败。TableCode={TableCode}", tableCode);
            throw;
        }
    }

    public async Task<IReadOnlyList<SyncBatch>> ListLatestByTableCodesAsync(IReadOnlyCollection<string> tableCodes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (tableCodes.Count == 0)
        {
            return Array.Empty<SyncBatch>();
        }

        var normalizedTableCodes = tableCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalizedTableCodes.Length == 0)
        {
            return Array.Empty<SyncBatch>();
        }

        var latestByTableCode = new Dictionary<string, SyncBatch>(StringComparer.OrdinalIgnoreCase);
        foreach (var suffix in await ListExistingShardSuffixesAsync(ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            var shardRows = await dbContext.SyncBatches
                .AsNoTracking()
                .Where(batch => normalizedTableCodes.Contains(batch.TableCode))
                .ToListAsync(ct);
            foreach (var entity in shardRows)
            {
                var candidate = MapToDomain(entity);
                if (!latestByTableCode.TryGetValue(candidate.TableCode, out var existing)
                    || ResolveReferenceTime(candidate) > ResolveReferenceTime(existing))
                {
                    latestByTableCode[candidate.TableCode] = candidate;
                }
            }
        }

        return latestByTableCode.Values
            .OrderBy(batch => batch.TableCode, StringComparer.Ordinal)
            .ToList();
    }

    private static void ValidateBatchForCreate(SyncBatch batch)
    {
        if (string.IsNullOrWhiteSpace(batch.BatchId))
        {
            throw new InvalidOperationException("BatchId 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(batch.TableCode))
        {
            throw new InvalidOperationException("TableCode 不能为空。");
        }
    }

    private async Task<LoadedBatchEntity> GetRequiredBatchFromShardsAsync(string batchId, CancellationToken ct)
    {
        var loaded = await TryGetBatchFromExistingShardsAsync(batchId, ct);
        if (loaded is not null)
        {
            return loaded.Value;
        }

        throw new InvalidOperationException($"未找到批次：{batchId}");
    }

    private async Task<LoadedBatchEntity?> TryGetBatchFromExistingShardsAsync(string batchId, CancellationToken ct)
    {
        var suffixes = await ListExistingShardSuffixesAsync(ct);
        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            var entity = await dbContext.SyncBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(batch => batch.BatchId == batchId, ct);
            if (entity is not null)
            {
                return new LoadedBatchEntity(suffix, entity);
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<string>> ListExistingShardSuffixesAsync(CancellationToken ct)
    {
        var tables = await shardTableResolver.ListPhysicalTablesAsync(SyncBatchLogicalTable, ct);
        return tables
            .Select(table => table[SyncBatchLogicalTable.Length..])
            .Where(static suffix => !string.IsNullOrWhiteSpace(suffix))
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(static suffix => suffix, StringComparer.Ordinal)
            .ToList();
    }

    private string ResolveBatchSuffix(DateTime timeLocal)
    {
        return shardSuffixResolver.ResolveLocal(timeLocal);
    }

    private static SyncBatchEntity MapToEntity(SyncBatch batch, SyncBatchStatus status)
    {
        return new SyncBatchEntity
        {
            BatchId = batch.BatchId,
            ParentBatchId = batch.ParentBatchId,
            TableCode = batch.TableCode,
            WindowStartLocal = batch.WindowStartLocal,
            WindowEndLocal = batch.WindowEndLocal,
            ReadCount = batch.ReadCount,
            InsertCount = batch.InsertCount,
            UpdateCount = batch.UpdateCount,
            DeleteCount = batch.DeleteCount,
            SkipCount = batch.SkipCount,
            Status = status,
            StartedTimeLocal = batch.StartedTimeLocal,
            CompletedTimeLocal = batch.CompletedTimeLocal,
            ErrorMessage = batch.ErrorMessage,
        };
    }

    private static SyncBatch MapToDomain(SyncBatchEntity entity)
    {
        return new SyncBatch
        {
            BatchId = entity.BatchId,
            ParentBatchId = entity.ParentBatchId,
            TableCode = entity.TableCode,
            WindowStartLocal = entity.WindowStartLocal,
            WindowEndLocal = entity.WindowEndLocal,
            ReadCount = entity.ReadCount,
            InsertCount = entity.InsertCount,
            UpdateCount = entity.UpdateCount,
            DeleteCount = entity.DeleteCount,
            SkipCount = entity.SkipCount,
            Status = entity.Status,
            StartedTimeLocal = entity.StartedTimeLocal,
            CompletedTimeLocal = entity.CompletedTimeLocal,
            ErrorMessage = entity.ErrorMessage
        };
    }

    private static DateTime ResolveReferenceTime(SyncBatch batch)
    {
        return batch.CompletedTimeLocal
            ?? batch.StartedTimeLocal
            ?? batch.WindowEndLocal;
    }

    /// <summary>
    /// 定义 LoadedBatchEntity 类型。
    /// </summary>
    private readonly record struct LoadedBatchEntity(string Suffix, SyncBatchEntity Entity);
}


