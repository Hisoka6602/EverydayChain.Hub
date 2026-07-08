using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IBusinessTaskMaterializer 类型。
/// </summary>
public interface IBusinessTaskMaterializer
{
    /// <summary>
    /// 执行 Materialize 方法。
    /// </summary>
    BusinessTaskEntity Materialize(BusinessTaskMaterializeRequest request);
}

