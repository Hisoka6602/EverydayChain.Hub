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
            CreatedAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync([trace], CancellationToken.None));
        Assert.Single(provisioner.EnsuredSuffixes);
        Assert.Equal("_202604", provisioner.EnsuredSuffixes[0]);
    }

    /// <summary>
    /// 相同月份重复写入应重复触发幂等建表，且不阻断写入流程入口。
    /// </summary>
    [Fact]
    public async Task WriteAsync_WithRepeatedMonthWrites_ShouldInvokeEnsureEachTime()
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
            CreatedAt = new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync([trace], CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync([trace], CancellationToken.None));

        Assert.Equal(2, provisioner.EnsuredSuffixes.Count);
        Assert.All(provisioner.EnsuredSuffixes, suffix => Assert.Equal("_202604", suffix));
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
