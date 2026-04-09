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
}
