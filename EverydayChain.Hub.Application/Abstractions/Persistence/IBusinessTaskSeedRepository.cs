using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 IBusinessTaskSeedRepository 类型。
/// </summary>
public interface IBusinessTaskSeedRepository
{
    /// <summary>
    /// 执行 InsertManualSeedAsync 方法。
    /// </summary>
    Task<BusinessTaskSeedResult> InsertManualSeedAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken);
}

