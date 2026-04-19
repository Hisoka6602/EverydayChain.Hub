using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 业务任务模拟补数仓储抽象。
/// </summary>
public interface IBusinessTaskSeedRepository
{
    /// <summary>
    /// 向指定业务任务分表批量插入模拟补数数据。
    /// </summary>
    /// <param name="command">补数命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>补数执行结果。</returns>
    Task<BusinessTaskSeedResult> InsertManualSeedAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken);
}
