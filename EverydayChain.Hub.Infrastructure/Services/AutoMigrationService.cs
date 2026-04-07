using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
    /// <summary>迁移基线固定 Schema。</summary>
    private const string ExpectedMigrationSchema = "dbo";

    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = shardingOptions.Value;

    /// <summary>危险动作门禁配置快照。</summary>
    private readonly DangerZoneOptions _dangerZoneOptions = dangerZoneOptions.Value;

    /// <inheritdoc/>
    public async Task RunAsync(CancellationToken cancellationToken) {
        LogConnectionSecuritySnapshot();
        ValidateMigrationSchemaOrThrow();

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
        var suffixes = BuildBootstrapSuffixes(resolver, localNow, _options.AutoCreateMonthsAhead);
        await shardTableProvisioner.EnsureShardTablesAsync(suffixes, cancellationToken);
    }

    /// <summary>
    /// 构建启动预建分表后缀集合（仅包含显式后缀分表，不包含无后缀基础表）。
    /// </summary>
    /// <param name="suffixResolver">后缀解析器。</param>
    /// <param name="localNow">当前本地时间。</param>
    /// <param name="monthsAhead">未来预建月数。</param>
    /// <returns>后缀列表。</returns>
    internal static IReadOnlyList<string> BuildBootstrapSuffixes(IShardSuffixResolver suffixResolver, DateTimeOffset localNow, int monthsAhead)
    {
        // 约定：空字符串代表“无后缀基础表”，当前策略明确只预建后缀分表，因此此处过滤空后缀。
        return suffixResolver.ResolveBootstrapSuffixes(localNow, monthsAhead)
            .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 校验迁移与运行时 Schema 一致性，不满足时阻断启动。
    /// </summary>
    /// <exception cref="InvalidOperationException">当 Schema 不是 dbo 时抛出。</exception>
    private void ValidateMigrationSchemaOrThrow() {
        if (string.Equals(_options.Schema, ExpectedMigrationSchema, StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        logger.LogError(
            "自动迁移配置阻断：当前 Sharding.Schema={ConfiguredSchema}，但初始迁移固定使用 {ExpectedSchema}。请将 Sharding.Schema 调整为 {ExpectedSchema}，避免迁移与运行时访问 Schema 不一致。",
            _options.Schema,
            ExpectedMigrationSchema,
            ExpectedMigrationSchema);
        throw new InvalidOperationException(
            $"自动迁移配置无效：当前 Sharding.Schema={_options.Schema}，初始迁移固定使用 {ExpectedMigrationSchema}。");
    }

    /// <summary>
    /// 输出连接参数快照（脱敏），便于定位 pre-login 握手失败。
    /// </summary>
    private void LogConnectionSecuritySnapshot() {
        try {
            var builder = new SqlConnectionStringBuilder(_options.ConnectionString);
            logger.LogInformation(
                "自动迁移连接快照(脱敏): DataSource={DataSource}, InitialCatalog={InitialCatalog}, Encrypt={Encrypt}, TrustServerCertificate={TrustServerCertificate}, IntegratedSecurity={IntegratedSecurity}, ConnectTimeout={ConnectTimeout}",
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
    /// 在目标库不存在时自动创建数据库，确保新库可执行后续迁移。
    /// </summary>
    /// <param name="dbContext">数据库上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task EnsureDatabaseCreatedAsync(HubDbContext dbContext, CancellationToken cancellationToken) {
        var dbConnection = dbContext.Database.GetDbConnection();
        var dataSource = dbConnection.DataSource;
        var initialCatalog = dbConnection.Database;
        var databaseCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
        if (await databaseCreator.ExistsAsync(cancellationToken)) {
            return;
        }

        if (!_dangerZoneOptions.AllowAutoCreateDatabase) {
            logger.LogError(
                "自动迁移阻断：检测到目标数据库不存在且 DangerZone.AllowAutoCreateDatabase=false。DataSource={DataSource}, InitialCatalog={InitialCatalog}",
                dataSource,
                initialCatalog);
            throw new InvalidOperationException("自动迁移阻断：目标数据库不存在，且未启用自动建库。");
        }

        logger.LogWarning(
            "自动迁移即将创建缺失数据库。DataSource={DataSource}, InitialCatalog={InitialCatalog}",
            dataSource,
            initialCatalog);
        await databaseCreator.CreateAsync(cancellationToken);
        logger.LogInformation(
            "自动迁移已成功创建数据库。DataSource={DataSource}, InitialCatalog={InitialCatalog}",
            dataSource,
            initialCatalog);
    }

    /// <summary>
    /// 输出待应用迁移清单，用于识别新迁移是否已纳入执行。
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
