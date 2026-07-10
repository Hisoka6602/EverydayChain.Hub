using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Controllers;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using HostRecirculationSummaryQueryRequest = EverydayChain.Hub.Host.Contracts.Requests.RecirculationSummaryQueryRequest;
using AppRecirculationSummaryQueryRequest = EverydayChain.Hub.Application.Models.RecirculationSummaryQueryRequest;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 RecirculationControllerTests 类型。
/// </summary>
public sealed class RecirculationControllerTests
{
    /// <summary>
    /// 执行 QuerySummaryAsync_ShouldReturnOk_WhenRequestIsValid 方法。
    /// </summary>
    [Fact]
    public async Task QuerySummaryAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        // 步骤：执行 QuerySummaryAsync_ShouldReturnOk_WhenRequestIsValid 方法的核心处理流程。
        var stubService = new StubRecirculationQueryService();
        var controller = new RecirculationController(stubService, new StubBusinessTaskReadService());
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

    /// <summary>
    /// 执行 ExportSummaryCsvAsync_ShouldReturnFile_WithChineseHeader 方法。
    /// </summary>
    [Fact]
    public async Task ExportSummaryCsvAsync_ShouldReturnFile_WithChineseHeader()
    {
        // 步骤：执行 ExportSummaryCsvAsync_ShouldReturnFile_WithChineseHeader 方法的核心处理流程。
        var stubService = new StubRecirculationQueryService();
        var controller = new RecirculationController(stubService, new StubBusinessTaskReadService());
        var request = new HostRecirculationSummaryQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            ChuteCode = "A-12",
            SortOrder = "Most"
        };

        var result = await controller.ExportSummaryCsvAsync(request, null, CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        var csvText = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("格口,波次号,回流数", csvText);
        Assert.DoesNotContain("Chute,WaveNo,Reflow", csvText);
    }

    /// <summary>
    /// 执行 ExportSummaryXlsxAsync_ShouldReturnFile_WhenRequestIsValid 方法。
    /// </summary>
    [Fact]
    public async Task ExportSummaryXlsxAsync_ShouldReturnFile_WhenRequestIsValid()
    {
        // 步骤：执行 ExportSummaryXlsxAsync_ShouldReturnFile_WhenRequestIsValid 方法的核心处理流程。
        var stubService = new StubRecirculationQueryService();
        var controller = new RecirculationController(stubService, new StubBusinessTaskReadService());
        var request = new HostRecirculationSummaryQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 21, 0, 0, 0), DateTimeKind.Local),
            ChuteCode = "A-12",
            SortOrder = "Most"
        };

        var result = await controller.ExportSummaryXlsxAsync(request, null, CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.NotEmpty(fileResult.FileContents);
    }

    /// <summary>
    /// 定义 StubRecirculationQueryService 类型。
    /// </summary>
    private sealed class StubRecirculationQueryService : IRecirculationQueryService
    {
        /// <summary>
        /// 获取或设置 LastRequest。
        /// </summary>
        public AppRecirculationSummaryQueryRequest? LastRequest { get; private set; }

        /// <summary>
        /// 执行 QuerySummaryAsync 方法。
        /// </summary>
        public Task<RecirculationSummaryQueryResult> QuerySummaryAsync(AppRecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            // 步骤：执行 QuerySummaryAsync 方法的核心处理流程。
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

        /// <summary>
        /// 执行 ExportCsvAsync 方法。
        /// </summary>
        public Task<string> ExportCsvAsync(AppRecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            // 步骤：执行 ExportCsvAsync 方法的核心处理流程。
            LastRequest = request;
            return Task.FromResult("格口,波次号,回流数\r\nA-12,WAVE-001,3\r\n");
        }
    }
}
