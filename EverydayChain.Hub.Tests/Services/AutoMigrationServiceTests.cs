using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Tests.Services.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 AutoMigrationServiceTests 类型。
/// </summary>
public class AutoMigrationServiceTests
{
    /// <summary>
    /// 存储 BaselineMigrationId 字段。
    /// </summary>
    private const string BaselineMigrationId = "20260418204107_RebuildHubBaselineV2";

    /// <summary>
    /// 存储 WaveCleanupAuditLogsMigrationId 字段。
    /// </summary>
    private const string WaveCleanupAuditLogsMigrationId = "20260707090000_AddWaveCleanupAuditLogs";

    /// <summary>
    /// 存储 RetentionCleanupAuditLogsMigrationId 字段。
    /// </summary>
    private const string RetentionCleanupAuditLogsMigrationId = "20260707150000_AddRetentionCleanupAuditLogs";

    [Fact]
    public void BuildBootstrapSuffixes_ShouldFilterEmptySuffix()
    {
        var suffixes = AutoMigrationService.BuildBootstrapSuffixes(
            new FixedBootstrapShardSuffixResolver(["_202604", "", "_202605", "_202604"]),
            DateTimeOffset.Now,
            1);

        Assert.Equal(2, suffixes.Count);
        Assert.Contains("_202604", suffixes);
        Assert.Contains("_202605", suffixes);
        Assert.DoesNotContain(string.Empty, suffixes);
    }

    [Fact]
    public void ShouldMarkBaselineMigration_ShouldReturnTrue_WhenOnlyBaselineExistsAndCoreTablesExist()
    {
        var shouldMark = AutoMigrationService.ShouldMarkBaselineMigration(
            [BaselineMigrationId],
            [],
            existingCoreTableCount: 2);

        Assert.True(shouldMark);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void ShouldMarkBaselineMigration_ShouldReturnFalse_WhenCoreTableAbsentOrAlreadyApplied(int existingCoreTableCount, bool applied)
    {
        var appliedMigrations = applied
            ? new[] { BaselineMigrationId }
            : Array.Empty<string>();
        var shouldMark = AutoMigrationService.ShouldMarkBaselineMigration(
            [BaselineMigrationId],
            appliedMigrations,
            existingCoreTableCount);

        Assert.False(shouldMark);
    }

    [Fact]
    public void HubDbContext_ShouldDiscoverCleanupAuditLogMigrations()
    {
        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseSqlServer("Server=127.0.0.1,1433;Database=EverydayChainHub_Test;User Id=sa;Password=Test123!;Encrypt=False;TrustServerCertificate=True;")
            .Options;
        var shardingOptions = Options.Create(new ShardingOptions
        {
            Schema = "dbo",
        });

        using var dbContext = new HubDbContext(options, shardingOptions);
        var migrations = dbContext.Database.GetMigrations().ToList();

        Assert.Contains(WaveCleanupAuditLogsMigrationId, migrations);
        Assert.Contains(RetentionCleanupAuditLogsMigrationId, migrations);
    }

    [Fact]
    public async Task ExecuteShardMaintenanceAsync_ShouldProvisionBeforeSynchronize()
    {
        var provisioner = new RecordingShardTableProvisioner();
        var synchronizer = new RecordingShardSchemaSynchronizer();
        var logger = new TestLogger<AutoMigrationService>();

        await AutoMigrationService.ExecuteShardMaintenanceAsync(
            provisioner,
            synchronizer,
            logger,
            ["_202604", "_202605"],
            CancellationToken.None);

        Assert.Equal(["_202604", "_202605"], provisioner.EnsuredSuffixes);
        Assert.Equal(1, synchronizer.SynchronizeAllCallCount);
        Assert.Collection(
            logger.Logs.Select(entry => entry.Message),
            message => Assert.Contains("开始预创建启动期分表", message, StringComparison.Ordinal),
            message => Assert.Contains("启动期分表预创建已完成", message, StringComparison.Ordinal),
            message => Assert.Contains("开始同步历史分表结构", message, StringComparison.Ordinal),
            message => Assert.Contains("历史分表结构同步已完成", message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteShardMaintenanceAsync_ShouldStillSynchronizeHistory_WhenSuffixesEmpty()
    {
        var provisioner = new RecordingShardTableProvisioner();
        var synchronizer = new RecordingShardSchemaSynchronizer();

        await AutoMigrationService.ExecuteShardMaintenanceAsync(
            provisioner,
            synchronizer,
            new TestLogger<AutoMigrationService>(),
            [],
            CancellationToken.None);

        Assert.Empty(provisioner.EnsuredSuffixes);
        Assert.Equal(1, synchronizer.SynchronizeAllCallCount);
    }
}
