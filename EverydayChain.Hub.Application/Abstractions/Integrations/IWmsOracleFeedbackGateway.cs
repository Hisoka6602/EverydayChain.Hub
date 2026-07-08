using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Integrations;

/// <summary>
/// 定义 IWmsOracleFeedbackGateway 类型。
/// </summary>
public interface IWmsOracleFeedbackGateway
{
    /// <summary>
    /// 执行 WriteFeedbackAsync 方法。
    /// </summary>
    Task<int> WriteFeedbackAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct);
}

