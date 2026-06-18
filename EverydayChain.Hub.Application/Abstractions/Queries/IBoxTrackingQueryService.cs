using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

public interface IBoxTrackingQueryService
{
    Task<BoxTrackingQueryResult> QueryAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken);
}
