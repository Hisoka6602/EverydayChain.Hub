using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

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

    /// <summary>
    /// 列出目标端轻量幂等状态。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>轻量状态集合。</returns>
    Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 按业务键集合删除目标数据。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="businessKeys">业务键集合。</param>
    /// <param name="deletionPolicy">删除策略。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>删除数量。</returns>
    Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct);

}
