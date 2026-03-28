using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EverydayChain.Hub.Infrastructure.Services;

public class SortingTaskTraceWriter(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    ISqlExecutionTuner tuner,
    ILogger<SortingTaskTraceWriter> logger) : ISortingTaskTraceWriter {
    public async Task WriteAsync(IReadOnlyCollection<Domain.Aggregates.SortingTaskTraceAggregate.SortingTaskTraceEntity> traces, CancellationToken cancellationToken) {
        if (traces.Count == 0) {
            return;
        }

        var grouped = traces.GroupBy(x => shardSuffixResolver.Resolve(x.CreatedAt));
        foreach (var group in grouped) {
            using var _ = TableSuffixScope.Use(group.Key);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var batchSize = Math.Max(1, tuner.CurrentBatchSize);
            var list = group.ToList();
            for (var i = 0; i < list.Count; i += batchSize) {
                var chunk = list.Skip(i).Take(batchSize).ToList();
                var stopwatch = Stopwatch.StartNew();
                var success = true;
                try {
                    await dbContext.SortingTaskTraces.AddRangeAsync(chunk, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex) {
                    success = false;
                    logger.LogError(ex, "批量写入分表失败，suffix={Suffix}, size={Size}", group.Key, chunk.Count);
                    throw;
                }
                finally {
                    stopwatch.Stop();
                    tuner.Record(stopwatch.Elapsed, success);
                }
            }
        }
    }
}
