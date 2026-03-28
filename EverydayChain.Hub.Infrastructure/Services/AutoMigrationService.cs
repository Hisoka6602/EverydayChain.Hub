using EverydayChain.Hub.Infrastructure.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

public class AutoMigrationService(
    HubDbContext dbContext,
    IShardSuffixResolver resolver,
    IShardTableManager shardTableManager,
    IOptions<ShardingOptions> shardingOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<AutoMigrationService> logger) : IAutoMigrationService {
    private readonly ShardingOptions _options = shardingOptions.Value;

    public async Task RunAsync(CancellationToken cancellationToken) {
        await dangerZoneExecutor.ExecuteAsync("auto-migrate-base", async token => {
            await dbContext.Database.MigrateAsync(token);
            logger.LogInformation("自动迁移: 基础迁移已执行完成。");
        }, cancellationToken);

        var suffixes = resolver.ResolveBootstrapSuffixes(DateTimeOffset.UtcNow, _options.AutoCreateMonthsAhead);
        await shardTableManager.EnsureShardTablesAsync(suffixes, cancellationToken);
    }
}
