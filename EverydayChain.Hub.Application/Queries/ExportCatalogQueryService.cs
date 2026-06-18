using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Queries;

public sealed class ExportCatalogQueryService : IExportCatalogQueryService
{
    public Task<ExportCatalogQueryResult> QueryAsync(ExportCatalogQueryRequest request, CancellationToken cancellationToken)
    {
        var updatedTimeLocal = DateTime.Now;
        var result = new ExportCatalogQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            GeneratedTimeLocal = updatedTimeLocal,
            Items =
            [
                new ExportCatalogItem
                {
                    Key = "sorting-report-csv",
                    Scope = "SortingReport",
                    Type = "Summary",
                    Content = "Dock-level sorting summary report",
                    Format = "CSV",
                    Endpoint = "/api/v1/reports/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-detail-csv",
                    Scope = "WaveData",
                    Type = "Detail",
                    Content = "Wave detail list with package totals, split totals, full-case totals, created time, and status",
                    Format = "CSV",
                    Endpoint = "/api/v1/waves/list/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-zone-detail-csv",
                    Scope = "ProgressDetail",
                    Type = "Detail",
                    Content = "Wave zone progress detail with totals, pending counts, progress, recirculation counts, and exception counts",
                    Format = "CSV",
                    Endpoint = "/api/v1/waves/zones/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "recirculation-summary-csv",
                    Scope = "Recirculation",
                    Type = "Summary",
                    Content = "Recirculation summary grouped by chute and wave",
                    Format = "CSV",
                    Endpoint = "/api/v1/recirculations/summary/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                }
            ]
        };
        return Task.FromResult(result);
    }
}
