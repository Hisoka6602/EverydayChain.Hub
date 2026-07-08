using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using HostExportCatalogQueryRequest = EverydayChain.Hub.Host.Contracts.Requests.ExportCatalogQueryRequest;
using AppExportCatalogQueryRequest = EverydayChain.Hub.Application.Models.ExportCatalogQueryRequest;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 ExportsControllerTests 类型。
/// </summary>
public sealed class ExportsControllerTests
{
    [Fact]
    public async Task QueryCatalogAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubExportCatalogQueryService();
        var controller = new ExportsController(stubService);
        var request = new HostExportCatalogQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local)
        };

        var result = await controller.QueryCatalogAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ExportCatalogResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal(2, response.Data!.Items.Count);
        Assert.Equal("wave-detail-csv", response.Data.Items[0].Key);
        Assert.Equal("wave-zone-detail-csv", response.Data.Items[1].Key);
        Assert.NotNull(stubService.LastRequest);
    }

    /// <summary>
    /// 定义 StubExportCatalogQueryService 类型。
    /// </summary>
    private sealed class StubExportCatalogQueryService : IExportCatalogQueryService
    {
        /// <summary>
        /// 获取或设置 LastRequest。
        /// </summary>
        public AppExportCatalogQueryRequest? LastRequest { get; private set; }

        public Task<ExportCatalogQueryResult> QueryAsync(AppExportCatalogQueryRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new ExportCatalogQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal,
                GeneratedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 12, 0, 0), DateTimeKind.Local),
                Items =
                [
                    new ExportCatalogItem
                    {
                        Key = "wave-detail-csv",
                        Scope = "WaveData",
                        Type = "Detail",
                        Content = "Wave detail list",
                        Format = "CSV",
                        Endpoint = "/api/v1/waves/list/export/csv",
                        UpdatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 12, 0, 0), DateTimeKind.Local)
                    },
                    new ExportCatalogItem
                    {
                        Key = "wave-zone-detail-csv",
                        Scope = "ProgressDetail",
                        Type = "Detail",
                        Content = "Wave zone progress detail",
                        Format = "CSV",
                        Endpoint = "/api/v1/waves/zones/export/csv",
                        UpdatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 12, 0, 0), DateTimeKind.Local)
                    }
                ]
            });
        }
    }
}

