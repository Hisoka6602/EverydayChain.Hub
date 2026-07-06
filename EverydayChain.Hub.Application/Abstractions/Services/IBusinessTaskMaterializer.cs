using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IBusinessTaskMaterializer
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    BusinessTaskEntity Materialize(BusinessTaskMaterializeRequest request);
}

