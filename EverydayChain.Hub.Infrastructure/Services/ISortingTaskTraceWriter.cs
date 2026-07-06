using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISortingTaskTraceWriter
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task WriteAsync(IReadOnlyCollection<SortingTaskTraceEntity> traces, CancellationToken cancellationToken);
}

