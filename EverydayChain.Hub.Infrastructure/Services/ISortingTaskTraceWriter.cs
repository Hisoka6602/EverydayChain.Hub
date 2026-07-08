using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 ISortingTaskTraceWriter 类型。
/// </summary>
public interface ISortingTaskTraceWriter
{
    /// <summary>
    /// 执行 WriteAsync 方法。
    /// </summary>
    Task WriteAsync(IReadOnlyCollection<SortingTaskTraceEntity> traces, CancellationToken cancellationToken);
}

