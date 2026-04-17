using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 分拣报表查询服务抽象。
/// </summary>
public interface ISortingReportQueryService
{
    /// <summary>
    /// 查询报表数据。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>报表结果。</returns>
    Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 导出 CSV 报表文本。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文本。</returns>
    Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken);
}
