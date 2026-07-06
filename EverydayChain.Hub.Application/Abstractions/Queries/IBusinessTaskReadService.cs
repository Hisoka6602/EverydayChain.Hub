using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IBusinessTaskReadService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskQueryResult> QueryTasksAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskQueryResult> QueryExceptionsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskQueryResult> QueryRecirculationsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken);
}

