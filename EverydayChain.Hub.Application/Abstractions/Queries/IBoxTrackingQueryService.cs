using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义 IBoxTrackingQueryService 类型。
/// </summary>
public interface IBoxTrackingQueryService
{
    /// <summary>
    /// 执行 QueryAsync 方法。
    /// </summary>
    Task<BoxTrackingQueryResult> QueryAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QueryAllAsync 方法。
    /// </summary>
    Task<IReadOnlyList<BoxTrackingItem>> QueryAllAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken);
}

