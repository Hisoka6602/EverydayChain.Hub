using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义 IDockDashboardQueryService 类型。
/// </summary>
public interface IDockDashboardQueryService
{
    /// <summary>
    /// 执行 QueryAsync 方法。
    /// </summary>
    Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken);
}

