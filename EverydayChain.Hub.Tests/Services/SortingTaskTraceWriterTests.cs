using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SortingTaskTraceWriterTests
{
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

