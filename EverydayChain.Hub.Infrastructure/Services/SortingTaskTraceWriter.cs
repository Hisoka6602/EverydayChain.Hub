using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Collections.Concurrent;
using SortingTaskTraceEntity = EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate.SortingTaskTraceEntity;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SortingTaskTraceWriter(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    ISqlExecutionTuner tuner,
    ILogger<SortingTaskTraceWriter> logger) : ISortingTaskTraceWriter
{
    private readonly ConcurrentDictionary<string, Lazy<Task>> _ensureTasks = new(StringComparer.Ordinal);

    public async Task WriteAsync(IReadOnlyCollection<SortingTaskTraceEntity> traces, CancellationToken cancellationToken)
    {
        if (traces.Count == 0)
        {
            return;
        }

        var grouped = traces.GroupBy(x => shardSuffixResolver.Resolve(x.CreatedAt));
        foreach (var group in grouped)
        {
            var lazyEnsure = _ensureTasks.GetOrAdd(
                group.Key,
                suffix => new Lazy<Task>(
                    () => shardTableProvisioner.EnsureShardTableAsync(suffix, CancellationToken.None),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            try
            {
                await lazyEnsure.Value.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                _ensureTasks.TryRemove(new KeyValuePair<string, Lazy<Task>>(group.Key, lazyEnsure));
                throw;
            }
            using var suffixScope = TableSuffixScope.Use(group.Key);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            var batchSize = Math.Max(1, tuner.CurrentBatchSize);
            var items = group.ToArray();
            for (var i = 0; i < items.Length; i += batchSize)
            {
                var count = Math.Min(batchSize, items.Length - i);
                var chunk = new ArraySegment<SortingTaskTraceEntity>(items, i, count);
                var stopwatch = Stopwatch.StartNew();
                var success = true;
                try
                {
                    await dbContext.SortingTaskTraces.AddRangeAsync(chunk, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    dbContext.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    success = false;
                    logger.LogError(ex, "批量写入分表失败，suffix={Suffix}, size={Size}", group.Key, chunk.Count);
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    tuner.Record(stopwatch.Elapsed, success);
                }
            }
        }
    }
}

