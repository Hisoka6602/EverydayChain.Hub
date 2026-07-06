using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IBusinessTaskSeedRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskSeedResult> InsertManualSeedAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken);
}

