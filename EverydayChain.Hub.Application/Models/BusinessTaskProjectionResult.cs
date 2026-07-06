using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class BusinessTaskProjectionResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<BusinessTaskEntity> Entities { get; set; } = [];
}

