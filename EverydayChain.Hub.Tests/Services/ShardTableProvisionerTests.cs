using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// ShardTableProvisioner 行为测试。
/// </summary>
public class ShardTableProvisionerTests
{
    /// <summary>
    /// 纳管逻辑表集合为空时应立即抛出异常。
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyManagedLogicalTables_ShouldThrow()
    {
        var options = Options.Create(new ShardingOptions());
        var action = () => _ = new ShardTableProvisioner(
            options,
            Array.Empty<string>(),
            NullLogger<ShardTableProvisioner>.Instance,
            new PassThroughDangerZoneExecutor());

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("纳管逻辑表集合为空", ex.Message);
    }

    /// <summary>
    /// 并发上限配置超出范围时应钳制后仍可执行。
    /// </summary>
    [Fact]
    public async Task EnsureShardTablesAsync_WithOutOfRangeConcurrency_ShouldComplete()
    {
        var options = Options.Create(new ShardingOptions
        {
            PreProvisionMaxConcurrency = 0
        });

        var provisioner = new ShardTableProvisioner(
            options,
            ["sorting_task_trace"],
            NullLogger<ShardTableProvisioner>.Instance,
            new PassThroughDangerZoneExecutor());

        await provisioner.EnsureShardTablesAsync([], CancellationToken.None);
    }
}
