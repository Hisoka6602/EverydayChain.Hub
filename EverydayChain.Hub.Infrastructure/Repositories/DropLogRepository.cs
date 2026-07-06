using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public class DropLogRepository(
    IDbContextFactory<HubDbContext> contextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner) : IDropLogRepository
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string DropLogLogicalTable = "drop_logs";

    public async Task SaveAsync(DropLogEntity entity, CancellationToken ct)
    {
        var suffix = shardSuffixResolver.ResolveLocal(entity.CreatedTimeLocal);
        await shardTableProvisioner.EnsureShardTableAsync(DropLogLogicalTable, suffix, ct);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.DropLogs.Add(entity);
        await db.SaveChangesAsync(ct);
    }
}

