using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 总是抛出异常的 DbContextFactory 桩实现。
/// </summary>
public sealed class ThrowingHubDbContextFactory : IDbContextFactory<HubDbContext>
{
    /// <inheritdoc />
    public Task<HubDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromException<HubDbContext>(new InvalidOperationException("测试桩：禁止创建真实 DbContext。"));
    }

    /// <inheritdoc />
    public HubDbContext CreateDbContext()
    {
        throw new InvalidOperationException("测试桩：禁止创建真实 DbContext。");
    }
}
