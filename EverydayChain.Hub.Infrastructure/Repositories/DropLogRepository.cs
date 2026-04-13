using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 落格日志仓储 EF Core 实现，写入 SQL Server 中的 <c>drop_logs</c> 固定表（非分片）。
/// </summary>
public class DropLogRepository : IDropLogRepository
{
    /// <summary>
    /// DbContext 工厂，每次操作均创建独立上下文以保证线程安全。
    /// </summary>
    private readonly IDbContextFactory<HubDbContext> _contextFactory;

    /// <summary>
    /// 初始化落格日志仓储。
    /// </summary>
    /// <param name="contextFactory">HubDbContext 工厂。</param>
    public DropLogRepository(IDbContextFactory<HubDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(DropLogEntity entity, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        db.DropLogs.Add(entity);
        await db.SaveChangesAsync(ct);
    }
}
