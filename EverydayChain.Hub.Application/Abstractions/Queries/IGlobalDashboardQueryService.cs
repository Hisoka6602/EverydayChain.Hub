using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 总看板查询服务抽象。
/// </summary>
public interface IGlobalDashboardQueryService
{
    /// <summary>
    /// 查询总看板统计结果。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>总看板统计结果。</returns>
    Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken);
}
