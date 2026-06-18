using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

public interface IRecirculationQueryService
{
    Task<RecirculationSummaryQueryResult> QuerySummaryAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken);

    Task<string> ExportCsvAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken);
}
