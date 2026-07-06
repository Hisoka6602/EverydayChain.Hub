using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using AppBoxTrackingQueryRequest = EverydayChain.Hub.Application.Models.BoxTrackingQueryRequest;
using HostBoxTrackingQueryRequest = EverydayChain.Hub.Host.Contracts.Requests.BoxTrackingQueryRequest;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BoxTrackingControllerTests
{
    [Fact]
    public async Task QueryAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubBoxTrackingQueryService();
        var controller = new BoxTrackingController(stubService);
        var request = new HostBoxTrackingQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            BoxId = "BOX-001"
        };

        var result = await controller.QueryAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<BoxTrackingResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Single(response.Data!.Items);
        Assert.Equal("BOX-001", response.Data.Items[0].BoxId);
        Assert.Equal("ORDER-001", response.Data.Items[0].OrderId);
        Assert.Equal("STORE-001", response.Data.Items[0].StoreId);
        Assert.Equal("Store One", response.Data.Items[0].StoreName);
        Assert.Equal("SKU-001", response.Data.Items[0].ProductCode);
        Assert.Equal("LOC-001", response.Data.Items[0].PickLocation);
        Assert.NotNull(stubService.LastRequest);
        Assert.Equal("BOX-001", stubService.LastRequest!.BoxId);
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class StubBoxTrackingQueryService : IBoxTrackingQueryService
    {
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public AppBoxTrackingQueryRequest? LastRequest { get; private set; }

        public Task<BoxTrackingQueryResult> QueryAsync(AppBoxTrackingQueryRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new BoxTrackingQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal,
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 50,
                Items =
                [
                    new BoxTrackingItem
                    {
                        BoxId = "BOX-001",
                        TaskCode = "TASK-001",
                        WaveCode = "WAVE-001",
                        OrderId = "ORDER-001",
                        StoreId = "STORE-001",
                        StoreName = "Store One",
                        ProductCode = "SKU-001",
                        PickLocation = "LOC-001",
                        Scanner = "SCN-01",
                        ScannedAtLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 0, 0), DateTimeKind.Local),
                        Chute = "B-07",
                        Status = "Scanned",
                        IsMatched = true
                    }
                ]
            });
        }

        public Task<IReadOnlyList<BoxTrackingItem>> QueryAllAsync(AppBoxTrackingQueryRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult<IReadOnlyList<BoxTrackingItem>>(
            [
                new BoxTrackingItem
                {
                    BoxId = "BOX-001",
                    TaskCode = "TASK-001",
                    WaveCode = "WAVE-001",
                    OrderId = "ORDER-001",
                    StoreId = "STORE-001",
                    StoreName = "Store One",
                    ProductCode = "SKU-001",
                    PickLocation = "LOC-001",
                    Scanner = "SCN-01",
                    ScannedAtLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 0, 0), DateTimeKind.Local),
                    Chute = "B-07",
                    Status = "Scanned",
                    IsMatched = true
                }
            ]);
        }
    }
}

