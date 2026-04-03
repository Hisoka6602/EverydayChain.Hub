using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Persistence;

/// <summary>
/// EF Core 设计时 DbContext 工厂，供 <c>dotnet ef migrations</c> 命令使用。
/// 使用默认连接字符串与空分表后缀创建基础上下文。
/// </summary>
public class DesignTimeHubDbContextFactory : IDesignTimeDbContextFactory<HubDbContext>
{
    /// <summary>
    /// 创建用于迁移设计时的 <see cref="HubDbContext"/> 实例。
    /// </summary>
    /// <param name="args">命令行参数（不使用）。</param>
    /// <returns>配置好连接字符串与分表模型缓存键工厂的 DbContext。</returns>
    public HubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HubDbContext>();
        var shardingOptions = new ShardingOptions();
        optionsBuilder.UseSqlServer(shardingOptions.ConnectionString);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ShardModelCacheKeyFactory>();

        using var _ = TableSuffixScope.Use(string.Empty);
        return new HubDbContext(optionsBuilder.Options, Microsoft.Extensions.Options.Options.Create(shardingOptions));
    }
}
