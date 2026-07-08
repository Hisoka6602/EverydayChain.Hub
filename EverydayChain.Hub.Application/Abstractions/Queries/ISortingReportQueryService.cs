using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义 ISortingReportQueryService 类型。
/// </summary>
public interface ISortingReportQueryService
{
    /// <summary>
    /// 执行 QueryAsync 方法。
    /// </summary>
    Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 ExportCsvAsync 方法。
    /// </summary>
    Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken);
}

