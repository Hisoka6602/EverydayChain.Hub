using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 业务回传补偿服务抽象，定义按任务与按批次重试失败回传的应用编排契约。
/// </summary>
public interface IFeedbackCompensationService
{
    /// <summary>
    /// 按任务编码重试单条失败回传记录。
    /// </summary>
    /// <param name="taskCode">业务任务编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次补偿执行结果。</returns>
    Task<FeedbackCompensationResult> RetryByTaskCodeAsync(string taskCode, CancellationToken ct);

    /// <summary>
    /// 按批次重试失败回传记录。
    /// </summary>
    /// <param name="batchSize">单次处理批次大小（建议范围：1~1000）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次补偿执行结果。</returns>
    Task<FeedbackCompensationResult> RetryFailedBatchAsync(int batchSize, CancellationToken ct);
}
