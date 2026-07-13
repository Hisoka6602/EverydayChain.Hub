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
                    Scope = "总看板",
                    Type = "汇总",
                    Content = "核心任务进度、读码率、同步进度与运营总览。",
                    Format = "CSV",
                    Endpoint = "/api/v1/dashboard/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "dashboard-summary-xlsx",
                    Scope = "总看板",
                    Type = "汇总",
                    Content = "核心任务进度、读码率、同步进度与运营总览。",
                    Format = "XLSX",
                    Endpoint = "/api/v1/dashboard/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "sorting-report-csv",
                    Scope = "分拣报表",
                    Type = "汇总",
                    Content = "码头维度分拣汇总报表。",
                    Format = "CSV",
                    Endpoint = "/api/v1/reports/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "sorting-report-xlsx",
                    Scope = "分拣报表",
                    Type = "汇总",
                    Content = "码头维度分拣汇总报表。",
                    Format = "XLSX",
                    Endpoint = "/api/v1/reports/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "dock-dashboard-csv",
                    Scope = "码头看板",
                    Type = "汇总",
                    Content = "码头看板汇总，包含待分拣数、回流数、异常数与进度。",
                    Format = "CSV",
                    Endpoint = "/api/v1/dock-dashboard/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "dock-dashboard-xlsx",
                    Scope = "码头看板",
                    Type = "汇总",
                    Content = "码头看板汇总，包含待分拣数、回流数、异常数与进度。",
                    Format = "XLSX",
                    Endpoint = "/api/v1/dock-dashboard/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-summary-csv",
                    Scope = "波次数据",
                    Type = "汇总",
                    Content = "波次汇总列表，包含包裹总数、待分拣数、拆零/整件未分拣数量、回流数与异常数。",
                    Format = "CSV",
                    Endpoint = "/api/v1/waves/list/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-summary-xlsx",
                    Scope = "波次数据",
                    Type = "汇总",
                    Content = "波次汇总列表，包含包裹总数、待分拣数、拆零/整件未分拣数量、回流数与异常数。",
                    Format = "XLSX",
                    Endpoint = "/api/v1/waves/list/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-detail-csv",
                    Scope = "波次数据",
                    Type = "明细",
                    Content = "波次任务级明细列表，包含条码、订单、门店、商品、拣货位、格口、扫描时间与状态。",
                    Format = "CSV",
                    Endpoint = "/api/v1/waves/details/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-detail-xlsx",
                    Scope = "波次数据",
                    Type = "明细",
                    Content = "波次任务级明细列表，包含条码、订单、门店、商品、拣货位、格口、扫描时间与状态。",
                    Format = "XLSX",
                    Endpoint = "/api/v1/waves/details/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-zone-detail-csv",
                    Scope = "进度详情",
                    Type = "明细",
                    Content = "波次分区进度明细，包含总数、待分拣数、进度、回流数与异常数。",
                    Format = "CSV",
                    Endpoint = "/api/v1/waves/zones/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "wave-zone-detail-xlsx",
                    Scope = "进度详情",
                    Type = "明细",
                    Content = "波次分区进度明细，包含总数、待分拣数、进度、回流数与异常数。",
                    Format = "XLSX",
                    Endpoint = "/api/v1/waves/zones/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "recirculation-summary-csv",
                    Scope = "回流",
                    Type = "汇总",
                    Content = "按格口和波次分组的回流汇总。",
                    Format = "CSV",
                    Endpoint = "/api/v1/recirculations/summary/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "recirculation-summary-xlsx",
                    Scope = "回流",
                    Type = "汇总",
                    Content = "按格口和波次分组的回流汇总。",
                    Format = "XLSX",
                    Endpoint = "/api/v1/recirculations/summary/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "box-tracking-csv",
                    Scope = "箱号追踪",
                    Type = "明细",
                    Content = "箱号追踪结果，包含订单、箱号、门店、扫描设备、扫描时间、格口与状态。",
                    Format = "CSV",
                    Endpoint = "/api/v1/box-tracking/export/csv",
                    UpdatedTimeLocal = updatedTimeLocal
                },
                new ExportCatalogItem
                {
                    Key = "box-tracking-xlsx",
                    Scope = "箱号追踪",
                    Type = "明细",
                    Content = "箱号追踪结果，包含订单、箱号、门店、扫描设备、扫描时间、格口与状态。",
                    Format = "XLSX",
                    Endpoint = "/api/v1/box-tracking/export/xlsx",
                    UpdatedTimeLocal = updatedTimeLocal
                }
            ]
        };
        return Task.FromResult(result);
    }
}

