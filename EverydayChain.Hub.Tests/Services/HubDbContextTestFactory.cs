using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// HubDbContext 测试工厂。
/// </summary>
/// <param name="contextOptions">数据库上下文选项。</param>
/// <param name="shardingOptions">分片配置。</param>
public sealed class HubDbContextTestFactory(
    DbContextOptions<HubDbContext> contextOptions,
    IOptions<ShardingOptions> shardingOptions) : IDbContextFactory<HubDbContext>
{
    /// <inheritdoc/>
    public HubDbContext CreateDbContext()
    {
        return new HubDbContext(contextOptions, shardingOptions);
    }
}
