using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IFeedbackCompensationService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<FeedbackCompensationResult> RetryByTaskCodeAsync(string taskCode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<FeedbackCompensationResult> RetryFailedBatchAsync(int batchSize, CancellationToken ct);
}

