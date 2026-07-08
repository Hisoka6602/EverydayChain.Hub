using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services.Sharding;
using System.Data;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 AutoMigrationService 类型。
/// </summary>
public class AutoMigrationService(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver resolver,
    IShardTableProvisioner shardTableProvisioner,
    IShardSchemaSynchronizer shardSchemaSynchronizer,
    IOptions<ShardingOptions> shardingOptions,
    IOptions<DangerZoneOptions> dangerZoneOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<AutoMigrationService> logger) : IAutoMigrationService {

    /// <summary>
    /// 存储 ExpectedMigrationSchema 字段。
    /// </summary>
    private const string ExpectedMigrationSchema = "dbo";
    /// <summary>
    /// 存储 BaselineMigrationId 字段。
    /// </summary>
    private const string BaselineMigrationId = "20260418204107_RebuildHubBaselineV2";
    /// <summary>
    /// 存储 BaselineProductVersion 字段。
    /// </summary>
    private const string BaselineProductVersion = "9.0.14";
    /// <summary>
    /// 存储 StartupMetadataProbeTimeoutSeconds 字段。
    /// </summary>
    private const int StartupMetadataProbeTimeoutSeconds = 15;

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly ShardingOptions _options = shardingOptions.Value;

    /// <summary>
    /// 存储 _dangerZoneOptions 字段。
    /// </summary>
    private readonly DangerZoneOptions _dangerZoneOptions = dangerZoneOptions.Value;

    /// <summary>
    /// 执行 RunAsync 方法。
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken) {
        // 步骤：执行 RunAsync 方法的核心处理流程。
        LogConnectionSecuritySnapshot();
        ValidateMigrationSchemaOrThrow();

        await dangerZoneExecutor.ExecuteAsync("auto-migrate-base", async token => {
            using var _ = TableSuffixScope.Use(string.Empty);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);
            await EnsureDatabaseCreatedAsync(dbContext, token);
            await TryMarkBaselineMigrationForExistingDatabaseAsync(dbContext, token);
            await LogPendingMigrationsAsync(dbContext, token);
            await dbContext.Database.MigrateAsync(token);
            logger.LogInformation("自动迁移: 基础迁移已执行完成。");
        }, cancellationToken, _dangerZoneOptions.AutoMigrateTimeoutSeconds);

        var localNow = DateTimeOffset.Now;
        var suffixes = BuildBootstrapSuffixes(resolver, localNow, _options.AutoCreateMonthsAhead);
        /// <summary>
        /// 执行 ExecuteShardMaintenanceAsync 方法。
        /// </summary>
        await ExecuteShardMaintenanceAsync(shardTableProvisioner, shardSchemaSynchronizer, logger, suffixes, cancellationToken);
    }

    /// <summary>
    /// 执行 BuildBootstrapSuffixes 方法。
    /// </summary>
    internal static IReadOnlyList<string> BuildBootstrapSuffixes(IShardSuffixResolver suffixResolver, DateTimeOffset localNow, int monthsAhead) {
        // 步骤：执行 BuildBootstrapSuffixes 方法的核心处理流程。
        return suffixResolver.ResolveBootstrapSuffixes(localNow, monthsAhead)
            .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 执行 ExecuteShardMaintenanceAsync 方法。
    /// </summary>
    internal static async Task ExecuteShardMaintenanceAsync(
        IShardTableProvisioner shardTableProvisioner,
        IShardSchemaSynchronizer shardSchemaSynchronizer,
        ILogger logger,
        IReadOnlyList<string> suffixes,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExecuteShardMaintenanceAsync 方法的核心处理流程。
        logger.LogInformation("自动迁移: 开始预创建启动期分表。SuffixCount={SuffixCount}", suffixes.Count);
        await shardTableProvisioner.EnsureShardTablesAsync(suffixes, cancellationToken);
        logger.LogInformation("自动迁移: 启动期分表预创建已完成。SuffixCount={SuffixCount}", suffixes.Count);

        logger.LogInformation("自动迁移: 开始同步历史分表结构。");
        await shardSchemaSynchronizer.SynchronizeAllAsync(cancellationToken);
        logger.LogInformation("自动迁移: 历史分表结构同步已完成。");
    }

    /// <summary>
    /// 执行 ShouldMarkBaselineMigration 方法。
    /// </summary>
    internal static bool ShouldMarkBaselineMigration(
        IReadOnlyList<string> allMigrations,
        IReadOnlyCollection<string> appliedMigrations,
        int existingCoreTableCount)
    {
        // 步骤：执行 ShouldMarkBaselineMigration 方法的核心处理流程。
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
    /// 执行 ValidateMigrationSchemaOrThrow 方法。
    /// </summary>
    private void ValidateMigrationSchemaOrThrow() {
        // 步骤：执行 ValidateMigrationSchemaOrThrow 方法的核心处理流程。
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
    /// 执行 LogConnectionSecuritySnapshot 方法。
    /// </summary>
    private void LogConnectionSecuritySnapshot() {
        // 步骤：执行 LogConnectionSecuritySnapshot 方法的核心处理流程。
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
    /// 执行 EnsureDatabaseCreatedAsync 方法。
    /// </summary>
    private async Task EnsureDatabaseCreatedAsync(HubDbContext dbContext, CancellationToken cancellationToken) {
        // 步骤：执行 EnsureDatabaseCreatedAsync 方法的核心处理流程。
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
    /// 执行 LogPendingMigrationsAsync 方法。
    /// </summary>
    private async Task LogPendingMigrationsAsync(HubDbContext dbContext, CancellationToken cancellationToken) {
        // 步骤：执行 LogPendingMigrationsAsync 方法的核心处理流程。
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingMigrationList = pendingMigrations.ToList();
        if (pendingMigrationList.Count == 0) {
            logger.LogInformation("自动迁移: 当前无待执行迁移。");
            return;
        }

        logger.LogInformation("自动迁移: 检测到待执行迁移：{PendingMigrations}", string.Join(", ", pendingMigrationList));
    }
}

