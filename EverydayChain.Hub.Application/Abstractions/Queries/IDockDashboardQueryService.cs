using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IDockDashboardQueryService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken);
}

