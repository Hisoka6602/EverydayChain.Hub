using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Integrations;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IWmsOracleFeedbackGateway
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<int> WriteFeedbackAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct);
}

