using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using HostRecirculationSummaryQueryRequest = EverydayChain.Hub.Host.Contracts.Requests.RecirculationSummaryQueryRequest;
using AppRecirculationSummaryQueryRequest = EverydayChain.Hub.Application.Models.RecirculationSummaryQueryRequest;

namespace EverydayChain.Hub.Tests.Host.Controllers;

public sealed class RecirculationControllerTests
{
    [Fact]
    public async Task QuerySummaryAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var stubService = new StubRecirculationQueryService();
        var controller = new RecirculationController(stubService);
        var request = new HostRecirculationSummaryQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            ChuteCode = "A-12",
            SortOrder = "Most"
        };

        var result = await controller.QuerySummaryAsync(request, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<RecirculationSummaryResponse>>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("A-12", response.Data!.SelectedChuteCode);
        Assert.Single(response.Data.Rows);
        Assert.Equal("A-12", response.Data.Rows[0].Chute);
        Assert.Equal("WAVE-001", response.Data.Rows[0].WaveNo);
        Assert.Equal(3, response.Data.Rows[0].Reflow);
        Assert.NotNull(stubService.LastRequest);
    }

    private sealed class StubRecirculationQueryService : IRecirculationQueryService
    {
        public AppRecirculationSummaryQueryRequest? LastRequest { get; private set; }

        public Task<RecirculationSummaryQueryResult> QuerySummaryAsync(AppRecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new RecirculationSummaryQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal,
                SelectedChuteCode = request.ChuteCode,
                SortOrder = request.SortOrder,
                Rows =
                [
                    new RecirculationSummaryRow
                    {
                        ChuteCode = "A-12",
                        WaveCode = "WAVE-001",
                        RecirculatedCount = 3
                    }
                ]
            });
        }

        public Task<string> ExportCsvAsync(AppRecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult("Chute,WaveNo,Reflow\r\nA-12,WAVE-001,3\r\n");
        }
    }
}
