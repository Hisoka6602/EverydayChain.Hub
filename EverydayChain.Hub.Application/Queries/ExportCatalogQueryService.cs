using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 定义 ExportCatalogQueryService 类型。
/// </summary>
public sealed class ExportCatalogQueryService : IExportCatalogQueryService
{
    /// <summary>
    /// 执行 QueryAsync 方法。
    /// </summary>
    public Task<ExportCatalogQueryResult> QueryAsync(ExportCatalogQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryAsync 方法的核心处理流程。
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
                    Key = "dashboard-summary-csv",
                    Scope = "Dashboard",
                    Type = "Summary",
                    Content = "Core task progress, recognition rate, sync progress, and operating overview.",
                    Format = "CSV",
                    Endpoint = "/api/v1/dashboard/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "dashboard-summary-xlsx",
                    Scope = "Dashboard",
                    Type = "Summary",
                    Content = "Core task progress, recognition rate, sync progress, and operating overview.",
                    Format = "XLSX",
                    Endpoint = "/api/v1/dashboard/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "sorting-report-csv",
                    Scope = "SortingReport",
                    Type = "Summary",
                    Content = "Dock-level sorting summary report.",
                    Format = "CSV",
                    Endpoint = "/api/v1/reports/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "sorting-report-xlsx",
                    Scope = "SortingReport",
                    Type = "Summary",
                    Content = "Dock-level sorting summary report.",
                    Format = "XLSX",
                    Endpoint = "/api/v1/reports/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "dock-dashboard-csv",
                    Scope = "DockDashboard",
                    Type = "Summary",
                    Content = "Dock dashboard summary with pending counts, recirculation counts, exception counts, and progress.",
                    Format = "CSV",
                    Endpoint = "/api/v1/dock-dashboard/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "dock-dashboard-xlsx",
                    Scope = "DockDashboard",
                    Type = "Summary",
                    Content = "Dock dashboard summary with pending counts, recirculation counts, exception counts, and progress.",
                    Format = "XLSX",
                    Endpoint = "/api/v1/dock-dashboard/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-summary-csv",
                    Scope = "WaveData",
                    Type = "Summary",
                    Content = "Wave summary list with package totals, pending counts, split/full ratios, recirculation counts, and exception counts.",
                    Format = "CSV",
                    Endpoint = "/api/v1/waves/list/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-summary-xlsx",
                    Scope = "WaveData",
                    Type = "Summary",
                    Content = "Wave summary list with package totals, pending counts, split/full ratios, recirculation counts, and exception counts.",
                    Format = "XLSX",
                    Endpoint = "/api/v1/waves/list/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-detail-csv",
                    Scope = "WaveData",
                    Type = "Detail",
                    Content = "Task-level wave detail list with barcode, order, store, product, pick location, chute, scan time, and status.",
                    Format = "CSV",
                    Endpoint = "/api/v1/waves/details/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-detail-xlsx",
                    Scope = "WaveData",
                    Type = "Detail",
                    Content = "Task-level wave detail list with barcode, order, store, product, pick location, chute, scan time, and status.",
                    Format = "XLSX",
                    Endpoint = "/api/v1/waves/details/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-zone-detail-csv",
                    Scope = "ProgressDetail",
                    Type = "Detail",
                    Content = "Wave zone progress detail with totals, pending counts, progress, recirculation counts, and exception counts.",
                    Format = "CSV",
                    Endpoint = "/api/v1/waves/zones/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-zone-detail-xlsx",
                    Scope = "ProgressDetail",
                    Type = "Detail",
                    Content = "Wave zone progress detail with totals, pending counts, progress, recirculation counts, and exception counts.",
                    Format = "XLSX",
                    Endpoint = "/api/v1/waves/zones/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "recirculation-summary-csv",
                    Scope = "Recirculation",
                    Type = "Summary",
                    Content = "Recirculation summary grouped by chute and wave.",
                    Format = "CSV",
                    Endpoint = "/api/v1/recirculations/summary/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "recirculation-summary-xlsx",
                    Scope = "Recirculation",
                    Type = "Summary",
                    Content = "Recirculation summary grouped by chute and wave.",
                    Format = "XLSX",
                    Endpoint = "/api/v1/recirculations/summary/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "box-tracking-csv",
                    Scope = "BoxTracking",
                    Type = "Detail",
                    Content = "Box-tracking result with order, box, store, scanner, scan time, chute, and status.",
                    Format = "CSV",
                    Endpoint = "/api/v1/box-tracking/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "box-tracking-xlsx",
                    Scope = "BoxTracking",
                    Type = "Detail",
                    Content = "Box-tracking result with order, box, store, scanner, scan time, chute, and status.",
                    Format = "XLSX",
                    Endpoint = "/api/v1/box-tracking/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                }
            ]
        };
        return Task.FromResult(result);
    }
}

