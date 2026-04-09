using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// SortingTaskTraceWriter 行为测试。
/// </summary>
public class SortingTaskTraceWriterTests
{
    /// <summary>
    /// 首次写入时应先触发分表兜底建表。
    /// </summary>
    [Fact]
    public async Task WriteAsync_WithSingleTrace_ShouldEnsureShardBeforeContextFactory()
    {
        var provisioner = new RecordingShardTableProvisioner();
        var writer = new SortingTaskTraceWriter(
            new ThrowingHubDbContextFactory(),
            new MonthShardSuffixResolver(),
            provisioner,
            new PassThroughSqlExecutionTuner(),
            NullLogger<SortingTaskTraceWriter>.Instance);

        var trace = new SortingTaskTraceEntity
        {
            BusinessNo = "B1",
            Channel = "C1",
            StationCode = "S1",
            Status = "Created",
            CreatedAt = new DateTimeOffset(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local))
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync([trace], CancellationToken.None));
        Assert.Single(provisioner.EnsuredSuffixes);
        Assert.Equal("_202604", provisioner.EnsuredSuffixes[0]);
    }

    /// <summary>
    /// 相同月份重复写入应仅首次触发幂等建表，后续走缓存不重复检查。
    /// </summary>
    [Fact]
    public async Task WriteAsync_WithRepeatedMonthWrites_ShouldCacheAndInvokeEnsureOnlyOnce()
    {
        var provisioner = new RecordingShardTableProvisioner();
        var writer = new SortingTaskTraceWriter(
            new ThrowingHubDbContextFactory(),
            new MonthShardSuffixResolver(),
            provisioner,
            new PassThroughSqlExecutionTuner(),
            NullLogger<SortingTaskTraceWriter>.Instance);

        var trace = new SortingTaskTraceEntity
        {
            BusinessNo = "B2",
            Channel = "C2",
            StationCode = "S2",
            Status = "Created",
            CreatedAt = new DateTimeOffset(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Local))
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync([trace], CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync([trace], CancellationToken.None));

        Assert.Single(provisioner.EnsuredSuffixes);
        Assert.Equal("_202604", provisioner.EnsuredSuffixes[0]);
    }

    /// <summary>
    /// 跨月份写入应分别触发各月份首次建表检查。
    /// </summary>
    [Fact]
    public async Task WriteAsync_WithDifferentMonthWrites_ShouldInvokeEnsurePerSuffix()
    {
        var provisioner = new RecordingShardTableProvisioner();
        var writer = new SortingTaskTraceWriter(
            new ThrowingHubDbContextFactory(),
            new MonthShardSuffixResolver(),
            provisioner,
            new PassThroughSqlExecutionTuner(),
            NullLogger<SortingTaskTraceWriter>.Instance);

        var aprilTrace = new SortingTaskTraceEntity
        {
            BusinessNo = "B3",
            Channel = "C3",
            StationCode = "S3",
            Status = "Created",
            CreatedAt = new DateTimeOffset(new DateTime(2026, 4, 30, 23, 0, 0, DateTimeKind.Local))
        };
        var mayTrace = new SortingTaskTraceEntity
        {
            BusinessNo = "B4",
            Channel = "C4",
            StationCode = "S4",
            Status = "Created",
            CreatedAt = new DateTimeOffset(new DateTime(2026, 5, 1, 0, 1, 0, DateTimeKind.Local))
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync([aprilTrace], CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync([mayTrace], CancellationToken.None));

        Assert.Equal(2, provisioner.EnsuredSuffixes.Count);
        Assert.Contains("_202604", provisioner.EnsuredSuffixes);
        Assert.Contains("_202605", provisioner.EnsuredSuffixes);
    }
}
