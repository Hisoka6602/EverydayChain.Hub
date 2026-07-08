using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IBusinessTaskSeedService 类型。
/// </summary>
public interface IBusinessTaskSeedService
{
    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    Task<BusinessTaskSeedResult> ExecuteAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken);
}

