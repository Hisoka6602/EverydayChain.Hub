using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using SortingTaskTraceEntity = EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate.SortingTaskTraceEntity;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 分拣任务追踪写入服务实现，按分表后缀分组后分批写入，并将结果回传给自动调谐器。
/// </summary>
public class SortingTaskTraceWriter(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardSuffixResolver shardSuffixResolver,
    ISqlExecutionTuner tuner,
    ILogger<SortingTaskTraceWriter> logger) : ISortingTaskTraceWriter
{
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
            using var _ = TableSuffixScope.Use(group.Key);
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
