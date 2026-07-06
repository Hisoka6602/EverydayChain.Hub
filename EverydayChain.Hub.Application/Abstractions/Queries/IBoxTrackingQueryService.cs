using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IBoxTrackingQueryService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BoxTrackingQueryResult> QueryAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BoxTrackingItem>> QueryAllAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken);
}

