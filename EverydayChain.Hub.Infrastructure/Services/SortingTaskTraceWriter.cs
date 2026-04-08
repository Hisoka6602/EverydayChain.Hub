using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Collections.Concurrent;
using SortingTaskTraceEntity = EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate.SortingTaskTraceEntity;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 分拣任务追踪写入服务实现，按分表后缀分组后分批写入，并将结果回传给自动调谐器。
/// </summary>
public class SortingTaskTraceWriter(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    ISqlExecutionTuner tuner,
    ILogger<SortingTaskTraceWriter> logger) : ISortingTaskTraceWriter
{
    /// <summary>
    /// 各后缀对应的建表确认任务缓存，键为分表后缀，值为首次调用 EnsureShardTableAsync 返回的 Task（惰性包装）。
    /// 后续并发调用会等待同一 Task 完成，避免在建表尚未结束时提前写入分表。
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<Task>> _ensureTasks = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public async Task WriteAsync(IReadOnlyCollection<SortingTaskTraceEntity> traces, CancellationToken cancellationToken)
    {
        if (traces.Count == 0)
        {
            return;
        }

        // 步骤1：按分表后缀分组，确保同一分表内的数据在同一 DbContext 下写入。
        var grouped = traces.GroupBy(x => shardSuffixResolver.Resolve(x.CreatedAt));
        foreach (var group in grouped)
        {
            // 使用 Lazy<Task> 保证同一后缀的建表操作仅执行一次：
            // 首次调用时 GetOrAdd 存储 Lazy<Task>；并发线程获取同一实例后均 await 其 Value，
            // LazyThreadSafetyMode.ExecutionAndPublication 确保工厂函数只被调用一次。
            // 建表失败时将该 Lazy 从字典移除，下次可重新触发建表。
            var lazyEnsure = _ensureTasks.GetOrAdd(
                group.Key,
                suffix => new Lazy<Task>(
                    () => shardTableProvisioner.EnsureShardTableAsync(suffix, cancellationToken),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            try
            {
                await lazyEnsure.Value;
            }
            catch
            {
                _ensureTasks.TryRemove(new KeyValuePair<string, Lazy<Task>>(group.Key, lazyEnsure));
                throw;
            }
            using var suffixScope = TableSuffixScope.Use(group.Key);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            // 步骤2：从调谐器获取当前批量写入窗口，分批执行写入。
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
                    // 步骤3：将本次写入的耗时与成功标志回传给调谐器进行窗口升降决策。
                    tuner.Record(stopwatch.Elapsed, success);
                }
            }
        }
    }
}
