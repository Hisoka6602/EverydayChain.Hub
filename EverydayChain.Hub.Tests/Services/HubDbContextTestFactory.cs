using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 HubDbContextTestFactory 类型。
/// </summary>
public sealed class HubDbContextTestFactory(
    DbContextOptions<HubDbContext> contextOptions,
    IOptions<ShardingOptions> shardingOptions) : IDbContextFactory<HubDbContext>
{
    public HubDbContext CreateDbContext()
    {
        return new HubDbContext(contextOptions, shardingOptions);
    }
}

