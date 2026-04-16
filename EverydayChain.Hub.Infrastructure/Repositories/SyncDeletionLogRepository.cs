using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.SyncDeletionLogAggregate;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步删除日志仓储 SQL Server 持久化实现（按月分片）。
/// </summary>
public class SyncDeletionLogRepository(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    ILogger<SyncDeletionLogRepository> logger) : ISyncDeletionLogRepository
{
    /// <summary>同步删除日志逻辑表名。</summary>
    private const string SyncDeletionLogLogicalTable = "sync_deletion_logs";

    /// <inheritdoc/>
    public async Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (logs.Count == 0)
        {
            return;
        }

        try
        {
            var stagedEntities = BuildStagedEntities(logs);
            var groupedEntities = stagedEntities
                .GroupBy(item => item.Suffix, item => item.Entity, StringComparer.Ordinal)
                .ToArray();
            foreach (var shardGroup in groupedEntities)
            {
                await shardTableProvisioner.EnsureShardTableAsync(SyncDeletionLogLogicalTable, shardGroup.Key, ct);
                using var scope = TableSuffixScope.Use(shardGroup.Key);
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
                dbContext.SyncDeletionLogs.AddRange(shardGroup);
                await dbContext.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "写入同步删除日志失败。Count={Count}", logs.Count);
            throw;
        }
    }

    /// <summary>
    /// 构建待持久化实体集合。
    /// </summary>
    /// <param name="logs">删除日志集合。</param>
    /// <returns>按分片后缀标注的实体集合。</returns>
    private List<StagedDeletionLogEntity> BuildStagedEntities(IReadOnlyList<SyncDeletionLog> logs)
    {
        var stagedEntities = new List<StagedDeletionLogEntity>(logs.Count);
        foreach (var log in logs)
        {
            var createdTimeLocal = DateTime.Now;
            var routingTimeLocal = log.DeletedTimeLocal ?? createdTimeLocal;
            var suffix = shardSuffixResolver.ResolveLocal(routingTimeLocal);
            stagedEntities.Add(new StagedDeletionLogEntity(
                suffix,
                new SyncDeletionLogEntity
                {
                    BatchId = log.BatchId,
                    ParentBatchId = log.ParentBatchId,
                    TableCode = log.TableCode,
                    BusinessKey = log.BusinessKey,
                    DeletionPolicy = log.DeletionPolicy,
                    Executed = log.Executed,
                    DeletedTimeLocal = log.DeletedTimeLocal,
                    SourceEvidence = log.SourceEvidence,
                    CreatedTimeLocal = createdTimeLocal
                }));
        }

        return stagedEntities;
    }

    /// <summary>
    /// 已分片的删除日志实体。
    /// </summary>
    /// <param name="Suffix">分片后缀。</param>
    /// <param name="Entity">持久化实体。</param>
    private readonly record struct StagedDeletionLogEntity(string Suffix, SyncDeletionLogEntity Entity);
}
