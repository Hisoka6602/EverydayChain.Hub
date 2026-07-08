using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IFeedbackCompensationService 类型。
/// </summary>
public interface IFeedbackCompensationService
{
    /// <summary>
    /// 执行 RetryByTaskCodeAsync 方法。
    /// </summary>
    Task<FeedbackCompensationResult> RetryByTaskCodeAsync(string taskCode, CancellationToken ct);

    /// <summary>
    /// 执行 RetryFailedBatchAsync 方法。
    /// </summary>
    Task<FeedbackCompensationResult> RetryFailedBatchAsync(int batchSize, CancellationToken ct);
}

