using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Integrations;

/// <summary>
/// WMS Oracle 业务回传网关抽象，定义向 Oracle WMS 写入业务回传结果的外部集成契约。
/// 实现类位于 <c>Infrastructure/Integrations</c>，运行时通过 DI 注入。
/// </summary>
public interface IWmsOracleFeedbackGateway
{
    /// <summary>
    /// 按业务键向 Oracle WMS 批量写入业务回传结果。
    /// </summary>
    /// <param name="tasks">待回传的业务任务列表。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际写入行数。</returns>
    Task<int> WriteFeedbackAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct);
}
