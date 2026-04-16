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
    /// <inheritdoc/>
    public async Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
    {
        var suffix = ResolveSuffix(entity.CreatedTimeLocal);
        await shardTableProvisioner.EnsureShardTableAsync(suffix, ct);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.ScanLogs.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 将本地时间解析为分片后缀。
    /// </summary>
    /// <param name="timeLocal">本地时间。</param>
    /// <returns>分片后缀。</returns>
    private string ResolveSuffix(DateTime timeLocal)
    {
        var normalizedLocalTime = timeLocal == DateTime.MinValue ? DateTime.Now : timeLocal;
        return shardSuffixResolver.Resolve(new DateTimeOffset(normalizedLocalTime));
    }
}
