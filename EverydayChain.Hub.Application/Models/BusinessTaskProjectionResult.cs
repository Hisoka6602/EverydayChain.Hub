using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务投影结果。
/// </summary>
public class BusinessTaskProjectionResult
{
    /// <summary>
    /// 投影后的业务任务实体集合。
    /// </summary>
    public IReadOnlyList<BusinessTaskEntity> Entities { get; set; } = [];
}
