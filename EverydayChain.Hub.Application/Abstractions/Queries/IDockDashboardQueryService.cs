using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 码头看板查询服务抽象。
/// </summary>
public interface IDockDashboardQueryService
{
    /// <summary>
    /// 查询码头看板统计。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>码头看板统计结果。</returns>
    Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken);
}
