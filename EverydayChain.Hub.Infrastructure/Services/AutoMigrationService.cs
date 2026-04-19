using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using System.Data;

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
    /// <summary>重建迁移基线 Id。</summary>
    private const string BaselineMigrationId = "20260418204107_RebuildHubBaselineV2";
    /// <summary>EF 迁移历史写入版本号。</summary>
    private const string BaselineProductVersion = "9.0.14";
    /// <summary>启动迁移阶段元数据探测 SQL 超时秒数（危险动作隔离器）。</summary>
    private const int StartupMetadataProbeTimeoutSeconds = 15;

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
            // 明确在“无后缀基础表”上下文执行迁移，避免外层异步上下文遗留后缀影响模型对比。
            using var _ = TableSuffixScope.Use(string.Empty);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);
            await EnsureDatabaseCreatedAsync(dbContext, token);
            await TryMarkBaselineMigrationForExistingDatabaseAsync(dbContext, token);
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
    internal static IReadOnlyList<string> BuildBootstrapSuffixes(IShardSuffixResolver suffixResolver, DateTimeOffset localNow, int monthsAhead) {
        // 约定：空字符串代表“无后缀基础表”，当前策略明确只预建后缀分表，因此此处过滤空后缀。
        return suffixResolver.ResolveBootstrapSuffixes(localNow, monthsAhead)
            .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 判断是否需要对存量库执行“基线迁移已应用”标记。
    /// 固定表 <c>business_tasks</c> 在此仅用于识别历史基线，不代表运行态写入目标。
    /// </summary>
    /// <param name="allMigrations">当前程序集全部迁移。</param>
    /// <param name="appliedMigrations">数据库已应用迁移。</param>
    /// <param name="existingCoreTableCount">已存在核心业务表数量。</param>
    /// <returns>需要标记返回 true，否则返回 false。</returns>
    internal static bool ShouldMarkBaselineMigration(
        IReadOnlyList<string> allMigrations,
        IReadOnlyCollection<string> appliedMigrations,
        int existingCoreTableCount)
    {
        if (allMigrations.Count != 1)
        {
            return false;
        }

        var onlyMigration = allMigrations[0];
        if (!string.Equals(onlyMigration, BaselineMigrationId, StringComparison.Ordinal))
        {
            return false;
        }

        if (appliedMigrations.Contains(onlyMigration))
        {
            return false;
        }

        return existingCoreTableCount > 0;
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
    /// 输出连接参数快照，便于定位 pre-login 握手失败。
    /// </summary>
    private void LogConnectionSecuritySnapshot() {
        try {
            var builder = new SqlConnectionStringBuilder(_options.ConnectionString);
            logger.LogInformation(
                "自动迁移连接快照: DataSource={DataSource}, InitialCatalog={InitialCatalog}, Encrypt={Encrypt}, TrustServerCertificate={TrustServerCertificate}, IntegratedSecurity={IntegratedSecurity}, ConnectTimeout={ConnectTimeout}",
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
    /// 针对迁移历史重建场景，在检测到存量业务表且仅存在基线迁移时自动写入迁移历史标记。
    /// </summary>
    /// <param name="dbContext">数据库上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task TryMarkBaselineMigrationForExistingDatabaseAsync(HubDbContext dbContext, CancellationToken cancellationToken)
    {
        var allMigrations = dbContext.Database.GetMigrations().ToList();
        var appliedMigrations = dbContext.Database.GetAppliedMigrations().ToList();
        var existingCoreTableCount = await CountExistingCoreTablesAsync(dbContext, cancellationToken);
        if (!ShouldMarkBaselineMigration(allMigrations, appliedMigrations, existingCoreTableCount))
        {
            return;
        }

        logger.LogWarning(
            "自动迁移检测到迁移基线重建场景：核心表已存在但基线迁移未标记，开始自动补写迁移历史。CoreTableCount={CoreTableCount}, BaselineMigration={BaselineMigration}",
            existingCoreTableCount,
            BaselineMigrationId);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[__EFMigrationsHistory]
                (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = {BaselineMigrationId})
            BEGIN
                INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                VALUES ({BaselineMigrationId}, {BaselineProductVersion});
            END;
            """,
            cancellationToken);

        logger.LogInformation("自动迁移已补写基线迁移历史。BaselineMigration={BaselineMigration}", BaselineMigrationId);
    }

    /// <summary>
    /// 统计当前数据库中已存在的核心业务表数量。
    /// 固定表 <c>business_tasks</c> 属于迁移遗留探测对象，正式运行写入目标始终为 <c>business_tasks_yyyyMM</c>。
    /// </summary>
    /// <param name="dbContext">数据库上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>核心表数量。</returns>
    private static async Task<int> CountExistingCoreTablesAsync(HubDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT COUNT(1)
                FROM sys.tables AS t
                INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                WHERE s.name = @schema
                  AND t.name IN (N'business_tasks');
                """;
            command.CommandTimeout = StartupMetadataProbeTimeoutSeconds;
            var schemaParameter = command.CreateParameter();
            schemaParameter.ParameterName = "@schema";
            schemaParameter.Value = ExpectedMigrationSchema;
            command.Parameters.Add(schemaParameter);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            return scalar is null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
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
