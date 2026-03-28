using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 分拣任务追踪写入服务接口，负责将追踪实体批量写入对应分表。
/// </summary>
public interface ISortingTaskTraceWriter
{
    /// <summary>
    /// 将追踪实体集合按分表后缀分组，批量写入目标分表。
    /// </summary>
    /// <param name="traces">待写入的追踪实体集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task WriteAsync(IReadOnlyCollection<SortingTaskTraceEntity> traces, CancellationToken cancellationToken);
}
