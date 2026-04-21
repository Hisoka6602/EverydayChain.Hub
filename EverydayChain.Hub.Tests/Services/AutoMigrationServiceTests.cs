using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Tests.Services.Sharding;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// AutoMigrationService 分表后缀策略测试。
/// </summary>
public class AutoMigrationServiceTests
{
    /// <summary>重建迁移基线 Id。</summary>
    private const string BaselineMigrationId = "20260418204107_RebuildHubBaselineV2";

    /// <summary>
    /// 启动预建后缀应过滤空后缀，仅保留分表后缀。
    /// </summary>
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

    /// <summary>
    /// 存量库且仅存在重建基线迁移时应触发基线标记。
    /// </summary>
    [Fact]
    public void ShouldMarkBaselineMigration_ShouldReturnTrue_WhenOnlyBaselineExistsAndCoreTablesExist()
    {
        var shouldMark = AutoMigrationService.ShouldMarkBaselineMigration(
            [BaselineMigrationId],
            [],
            existingCoreTableCount: 2);

        Assert.True(shouldMark);
    }

    /// <summary>
    /// 新库或已标记场景不应触发基线标记。
    /// </summary>
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

    /// <summary>
    /// 启动迁移链路应先预建分表，再同步历史分表结构。
    /// </summary>
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
}
