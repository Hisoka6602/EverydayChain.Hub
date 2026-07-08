using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义 IGlobalDashboardQueryService 类型。
/// </summary>
public interface IGlobalDashboardQueryService
{
    /// <summary>
    /// 执行 QueryAsync 方法。
    /// </summary>
    Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken);
}

