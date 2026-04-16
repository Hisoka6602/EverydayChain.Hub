using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.SyncChangeLogAggregate;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步变更日志仓储 SQL Server 持久化实现（按月分片）。
/// </summary>
public class SyncChangeLogRepository(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    ILogger<SyncChangeLogRepository> logger) : ISyncChangeLogRepository
{
    /// <summary>同步变更日志逻辑表名。</summary>
    private const string SyncChangeLogLogicalTable = "sync_change_logs";

    /// <inheritdoc/>
    public async Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (changes.Count == 0)
        {
            return;
        }

        try
        {
            var stagedEntities = BuildStagedEntities(changes);
            var groupedEntities = stagedEntities
                .GroupBy(item => item.Suffix, item => item.Entity, StringComparer.Ordinal)
                .ToArray();
            foreach (var shardGroup in groupedEntities)
            {
                await shardTableProvisioner.EnsureShardTableAsync(SyncChangeLogLogicalTable, shardGroup.Key, ct);
                using var scope = TableSuffixScope.Use(shardGroup.Key);
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
                dbContext.SyncChangeLogs.AddRange(shardGroup);
                await dbContext.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "写入同步变更日志失败。Count={Count}", changes.Count);
            throw;
        }
    }

    /// <summary>
    /// 构建待持久化实体集合。
    /// </summary>
    /// <param name="changes">变更日志集合。</param>
    /// <returns>按分片后缀标注的实体集合。</returns>
    private List<StagedChangeLogEntity> BuildStagedEntities(IReadOnlyList<SyncChangeLog> changes)
    {
        var stagedEntities = new List<StagedChangeLogEntity>(changes.Count);
        foreach (var change in changes)
        {
            var createdTimeLocal = DateTime.Now;
            var suffix = shardSuffixResolver.ResolveLocal(change.ChangedTimeLocal);
            stagedEntities.Add(new StagedChangeLogEntity(
                suffix,
                new SyncChangeLogEntity
                {
                    BatchId = change.BatchId,
                    ParentBatchId = change.ParentBatchId,
                    TableCode = change.TableCode,
                    OperationType = change.OperationType,
                    BusinessKey = change.BusinessKey,
                    BeforeSnapshot = change.BeforeSnapshot,
                    AfterSnapshot = change.AfterSnapshot,
                    ChangedTimeLocal = change.ChangedTimeLocal,
                    CreatedTimeLocal = createdTimeLocal
                }));
        }

        return stagedEntities;
    }

    /// <summary>
    /// 已分片的变更日志实体。
    /// </summary>
    /// <param name="Suffix">分片后缀。</param>
    /// <param name="Entity">持久化实体。</param>
    private readonly record struct StagedChangeLogEntity(string Suffix, SyncChangeLogEntity Entity);
}
