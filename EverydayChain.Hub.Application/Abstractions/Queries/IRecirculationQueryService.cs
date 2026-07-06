using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IRecirculationQueryService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<RecirculationSummaryQueryResult> QuerySummaryAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<string> ExportCsvAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken);
}

