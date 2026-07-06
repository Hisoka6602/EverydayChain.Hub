using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ThrowingHubDbContextFactory : IDbContextFactory<HubDbContext>
{
    public Task<HubDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromException<HubDbContext>(new InvalidOperationException("测试桩：禁止创建真实 DbContext。"));
    }

    public HubDbContext CreateDbContext()
    {
        throw new InvalidOperationException("测试桩：禁止创建真实 DbContext。");
    }
}

