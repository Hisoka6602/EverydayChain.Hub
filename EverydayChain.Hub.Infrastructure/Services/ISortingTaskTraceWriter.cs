using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;

namespace EverydayChain.Hub.Infrastructure.Services;

public interface ISortingTaskTraceWriter {
    Task WriteAsync(IReadOnlyCollection<SortingTaskTraceEntity> traces, CancellationToken cancellationToken);
}
