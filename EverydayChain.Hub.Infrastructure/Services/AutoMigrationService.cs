using EverydayChain.Hub.Infrastructure.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 自动迁移服务实现，在应用启动时执行 EF Core 基础迁移并预创建当前及未来分表。
/// </summary>
public class AutoMigrationService(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver resolver,
    IShardTableProvisioner shardTableProvisioner,
    IOptions<ShardingOptions> shardingOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<AutoMigrationService> logger) : IAutoMigrationService
{
    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = shardingOptions.Value;

    /// <inheritdoc/>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // 步骤1：通过隔离器执行 EF Core 基础 Migration。
        await dangerZoneExecutor.ExecuteAsync("auto-migrate-base", async token =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);
            await dbContext.Database.MigrateAsync(token);
            logger.LogInformation("自动迁移: 基础迁移已执行完成。");
        }, cancellationToken);

        // 步骤2：解析当前及未来分表后缀，预创建分表结构。
        var localNow = DateTimeOffset.Now;
        var suffixes = resolver.ResolveBootstrapSuffixes(localNow, _options.AutoCreateMonthsAhead);
        await shardTableProvisioner.EnsureShardTablesAsync(suffixes, cancellationToken);
    }
}
