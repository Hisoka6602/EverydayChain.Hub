using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IGlobalDashboardQueryService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken);
}

