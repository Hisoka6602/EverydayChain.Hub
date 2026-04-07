using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore.Storage;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 自动迁移服务实现，在应用启动时执行 EF Core 基础迁移并预创建当前及未来分表。
/// </summary>
public class AutoMigrationService(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver resolver,
    IShardTableProvisioner shardTableProvisioner,
    IOptions<ShardingOptions> shardingOptions,
    IOptions<DangerZoneOptions> dangerZoneOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<AutoMigrationService> logger) : IAutoMigrationService {

    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = shardingOptions.Value;

    /// <summary>危险动作门禁配置快照。</summary>
    private readonly DangerZoneOptions _dangerZoneOptions = dangerZoneOptions.Value;

    /// <inheritdoc/>
    public async Task RunAsync(CancellationToken cancellationToken) {
        LogConnectionSecuritySnapshot();

        // 步骤1：通过隔离器执行 EF Core 基础 Migration。
        await dangerZoneExecutor.ExecuteAsync("auto-migrate-base", async token => {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);
            await EnsureDatabaseCreatedAsync(dbContext, token);
            await LogPendingMigrationsAsync(dbContext, token);
            await dbContext.Database.MigrateAsync(token);
            logger.LogInformation("自动迁移: 基础迁移已执行完成。");
        }, cancellationToken, _dangerZoneOptions.AutoMigrateTimeoutSeconds);

        // 步骤2：解析当前及未来分表后缀，预创建分表结构。
        var localNow = DateTimeOffset.Now;
        var suffixes = resolver.ResolveBootstrapSuffixes(localNow, _options.AutoCreateMonthsAhead)
            .Append(string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        await shardTableProvisioner.EnsureShardTablesAsync(suffixes, cancellationToken);
    }

    /// <summary>
    /// 输出连接参数快照（不脱敏），便于定位 pre-login 握手失败。
    /// </summary>
    private void LogConnectionSecuritySnapshot() {
        try {
            var builder = new SqlConnectionStringBuilder(_options.ConnectionString);
            logger.LogInformation(
                "自动迁移连接快照(明文): ConnectionString={ConnectionString}, DataSource={DataSource}, InitialCatalog={InitialCatalog}, Encrypt={Encrypt}, TrustServerCertificate={TrustServerCertificate}, IntegratedSecurity={IntegratedSecurity}, ConnectTimeout={ConnectTimeout}",
                _options.ConnectionString,
                builder.DataSource,
                builder.InitialCatalog,
                builder.Encrypt,
                builder.TrustServerCertificate,
                builder.IntegratedSecurity,
                builder.ConnectTimeout);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "自动迁移连接串解析失败，跳过连接安全参数快照输出。");
        }
    }

    /// <summary>
    /// 在目标库不存在时自动创建数据库，确保新库可直接执行迁移。
    /// </summary>
    /// <param name="dbContext">数据库上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task EnsureDatabaseCreatedAsync(HubDbContext dbContext, CancellationToken cancellationToken) {
        var databaseCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
        if (await databaseCreator.ExistsAsync(cancellationToken)) {
            return;
        }

        await databaseCreator.CreateAsync(cancellationToken);
        logger.LogInformation("自动迁移: 检测到目标数据库不存在，已自动创建数据库。");
    }

    /// <summary>
    /// 输出待应用迁移清单，便于识别新迁移是否已纳入执行。
    /// </summary>
    /// <param name="dbContext">数据库上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task LogPendingMigrationsAsync(HubDbContext dbContext, CancellationToken cancellationToken) {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingMigrationList = pendingMigrations.ToList();
        if (pendingMigrationList.Count == 0) {
            logger.LogInformation("自动迁移: 当前无待执行迁移。");
            return;
        }

        logger.LogInformation("自动迁移: 检测到待执行迁移：{PendingMigrations}", string.Join(", ", pendingMigrationList));
    }
}
