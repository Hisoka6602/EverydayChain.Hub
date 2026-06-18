using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

public interface IExportCatalogQueryService
{
    Task<ExportCatalogQueryResult> QueryAsync(ExportCatalogQueryRequest request, CancellationToken cancellationToken);
}
