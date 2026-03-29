using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Repositories;

/// <summary>
/// 同步幂等合并仓储接口。
/// </summary>
public interface ISyncUpsertRepository
{
    /// <summary>
    /// 执行幂等合并。
    /// </summary>
    /// <param name="request">合并请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>合并结果。</returns>
    Task<SyncMergeResult> MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct);
}
