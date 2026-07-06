using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISortingReportQueryService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken);
}

