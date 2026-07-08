using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义 IBusinessTaskReadService 类型。
/// </summary>
public interface IBusinessTaskReadService
{
    /// <summary>
    /// 执行 QueryTasksAsync 方法。
    /// </summary>
    Task<BusinessTaskQueryResult> QueryTasksAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QueryExceptionsAsync 方法。
    /// </summary>
    Task<BusinessTaskQueryResult> QueryExceptionsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QueryRecirculationsAsync 方法。
    /// </summary>
    Task<BusinessTaskQueryResult> QueryRecirculationsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken);
}

