using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 业务任务物化服务，负责将同步链路输入映射为统一业务任务实体。
/// </summary>
public interface IBusinessTaskMaterializer
{
    /// <summary>
    /// 将输入数据物化为业务任务实体，并补齐默认状态与本地时间字段。
    /// </summary>
    /// <param name="request">物化输入模型。</param>
    /// <returns>已完成默认赋值的业务任务实体。</returns>
    BusinessTaskEntity Materialize(BusinessTaskMaterializeRequest request);
}
