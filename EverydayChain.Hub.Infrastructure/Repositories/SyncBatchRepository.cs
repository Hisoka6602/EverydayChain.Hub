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
/// 同步批次仓储 SQL Server 持久化实现（按月分片）。
/// </summary>
public class SyncBatchRepository(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IShardTableResolver shardTableResolver,
    ILogger<SyncBatchRepository> logger) : ISyncBatchRepository
{
    /// <summary>同步批次逻辑表名。</summary>
    private const string SyncBatchLogicalTable = "sync_batches";

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <summary>
    /// 校验创建批次入参。
    /// </summary>
    /// <param name="batch">批次对象。</param>
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

    /// <summary>
    /// 获取必须存在的批次。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>分片批次加载结果。</returns>
    private async Task<LoadedBatchEntity> GetRequiredBatchFromShardsAsync(string batchId, CancellationToken ct)
    {
        var loaded = await TryGetBatchFromExistingShardsAsync(batchId, ct);
        if (loaded is not null)
        {
            return loaded.Value;
        }

        throw new InvalidOperationException($"未找到批次：{batchId}");
    }

    /// <summary>
    /// 尝试从现有分片中加载批次。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>加载结果。</returns>
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

    /// <summary>
    /// 列出已存在同步批次分片后缀。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>后缀列表（倒序）。</returns>
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

    /// <summary>
    /// 解析批次分表后缀。
    /// </summary>
    /// <param name="timeLocal">本地时间。</param>
    /// <returns>分表后缀。</returns>
    private string ResolveBatchSuffix(DateTime timeLocal)
    {
        return shardSuffixResolver.ResolveLocal(timeLocal);
    }

    /// <summary>
    /// 将领域批次模型映射为持久化实体。
    /// </summary>
    /// <param name="batch">领域批次对象。</param>
    /// <param name="status">目标状态。</param>
    /// <returns>持久化实体。</returns>
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

    /// <summary>
    /// 分片批次加载结果。
    /// </summary>
    /// <param name="Suffix">分片后缀。</param>
    /// <param name="Entity">批次实体。</param>
    private readonly record struct LoadedBatchEntity(string Suffix, SyncBatchEntity Entity);
}
