using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskProjectionResult 类型。
/// </summary>
public class BusinessTaskProjectionResult
{
    /// <summary>
    /// 获取或设置 Entities。
    /// </summary>
    public IReadOnlyList<BusinessTaskEntity> Entities { get; set; } = [];
}

