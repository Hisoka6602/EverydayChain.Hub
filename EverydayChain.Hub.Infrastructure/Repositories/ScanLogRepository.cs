using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 扫描日志仓储 EF Core 实现，按月写入 <c>scan_logs_{yyyyMM}</c> 分表。
/// </summary>
public class ScanLogRepository(
    IDbContextFactory<HubDbContext> contextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner) : IScanLogRepository
{
    /// <summary>扫描日志逻辑表名。</summary>
    private const string ScanLogLogicalTable = "scan_logs";

    /// <inheritdoc/>
    public async Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
    {
        var suffix = shardSuffixResolver.ResolveLocal(entity.CreatedTimeLocal);
        await shardTableProvisioner.EnsureShardTableAsync(ScanLogLogicalTable, suffix, ct);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.ScanLogs.Add(entity);
        await db.SaveChangesAsync(ct);
    }
}
