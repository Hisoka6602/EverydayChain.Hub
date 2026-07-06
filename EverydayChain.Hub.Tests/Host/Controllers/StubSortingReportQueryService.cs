using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
internal sealed class StubSortingReportQueryService : ISortingReportQueryService
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public SortingReportQueryRequest? LastRequest { get; private set; }

    public SortingReportQueryResult Result { get; set; } = new()
    {
        StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local),
        EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 0, 0, 0), DateTimeKind.Local),
        Rows = [new SortingReportRow { DockCode = "7", SplitTotalCount = 1, FullCaseTotalCount = 2 }]
    };

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string CsvContent { get; set; } = "码头号,拆零总数,整件总数,拆零分拣数,整件分拣数,回流数,异常数\n7,1,2,0,0,0,0";

    public Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }

    public Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(CsvContent);
    }
}

