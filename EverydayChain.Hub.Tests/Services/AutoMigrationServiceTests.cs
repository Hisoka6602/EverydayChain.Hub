using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// AutoMigrationService 分表后缀策略测试。
/// </summary>
public class AutoMigrationServiceTests
{
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
            ["20260417185400_RebuildHubBaseline"],
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
            ? new[] { "20260417185400_RebuildHubBaseline" }
            : Array.Empty<string>();
        var shouldMark = AutoMigrationService.ShouldMarkBaselineMigration(
            ["20260417185400_RebuildHubBaseline"],
            appliedMigrations,
            existingCoreTableCount);

        Assert.False(shouldMark);
    }
}
