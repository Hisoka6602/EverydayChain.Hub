using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 分拣报表查询服务替身。
/// </summary>
internal sealed class StubSortingReportQueryService : ISortingReportQueryService
{
    /// <summary>
    /// 最近一次查询请求。
    /// </summary>
    public SortingReportQueryRequest? LastRequest { get; private set; }

    /// <summary>
    /// 固定查询结果。
    /// </summary>
    public SortingReportQueryResult Result { get; set; } = new()
    {
        StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
        EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
        Rows = [new SortingReportRow { DockCode = "7", SplitTotalCount = 1, FullCaseTotalCount = 2 }]
    };

    /// <summary>
    /// 固定 CSV 文本。
    /// </summary>
    public string CsvContent { get; set; } = "码头号,拆零总数\n7,1";

    /// <inheritdoc/>
    public Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }

    /// <inheritdoc/>
    public Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(CsvContent);
    }
}
