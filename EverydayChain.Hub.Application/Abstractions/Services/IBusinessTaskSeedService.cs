using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 业务任务模拟补数应用服务抽象。
/// </summary>
public interface IBusinessTaskSeedService
{
    /// <summary>
    /// 执行模拟补数。
    /// </summary>
    /// <param name="command">补数命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>补数执行结果。</returns>
    Task<BusinessTaskSeedResult> ExecuteAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken);
}
