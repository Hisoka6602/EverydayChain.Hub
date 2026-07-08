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
/// 定义 SyncChangeLogRepository 类型。
/// </summary>
public class SyncChangeLogRepository(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    ILogger<SyncChangeLogRepository> logger) : ISyncChangeLogRepository
{
    /// <summary>
    /// 存储 SyncChangeLogLogicalTable 字段。
    /// </summary>
    private const string SyncChangeLogLogicalTable = "sync_change_logs";

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

    private List<StagedChangeLogEntity> BuildStagedEntities(IReadOnlyList<SyncChangeLog> changes)
    {
        var stagedEntities = new List<StagedChangeLogEntity>(changes.Count);
        var createdTimeLocal = DateTime.Now;
        foreach (var change in changes)
        {
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
    /// 定义 StagedChangeLogEntity 类型。
    /// </summary>
    private readonly record struct StagedChangeLogEntity(string Suffix, SyncChangeLogEntity Entity);
}


