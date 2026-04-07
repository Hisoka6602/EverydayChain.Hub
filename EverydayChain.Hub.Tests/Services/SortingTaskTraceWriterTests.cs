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
            new ThrowingDbContextFactory(),
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
    public async Task WriteAsync_WithRepeatedMonthWrites_ShouldInvokeEnsureOnlyOnce()
    {
        var provisioner = new RecordingShardTableProvisioner();
        var writer = new SortingTaskTraceWriter(
            new ThrowingDbContextFactory(),
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
            new ThrowingDbContextFactory(),
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

    /// <summary>
    /// 记录建表调用的分表预建器桩实现。
    /// </summary>
    private sealed class RecordingShardTableProvisioner : IShardTableProvisioner
    {
        /// <summary>已触发建表的后缀列表。</summary>
        public List<string> EnsuredSuffixes { get; } = [];

        /// <inheritdoc />
        public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken)
        {
            EnsuredSuffixes.Add(suffix);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
        {
            foreach (var suffix in suffixes)
            {
                EnsuredSuffixes.Add(suffix);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 恒定批大小调谐器桩实现。
    /// </summary>
    private sealed class PassThroughSqlExecutionTuner : ISqlExecutionTuner
    {
        /// <inheritdoc />
        public int CurrentBatchSize => 100;

        /// <inheritdoc />
        public void Record(TimeSpan elapsed, bool success)
        {
        }
    }

    /// <summary>
    /// 总是抛出异常的 DbContextFactory 桩实现。
    /// </summary>
    private sealed class ThrowingDbContextFactory : IDbContextFactory<HubDbContext>
    {
        /// <inheritdoc />
        public Task<HubDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<HubDbContext>(new InvalidOperationException("测试桩：禁止创建真实 DbContext。"));
        }

        /// <inheritdoc />
        public HubDbContext CreateDbContext()
        {
            throw new InvalidOperationException("测试桩：禁止创建真实 DbContext。");
        }
    }
}
