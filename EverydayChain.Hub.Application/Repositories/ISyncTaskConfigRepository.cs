using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Repositories;

/// <summary>
/// 同步任务配置仓储接口。
/// </summary>
public interface ISyncTaskConfigRepository
{
    /// <summary>
    /// 按表编码获取配置。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>同步表定义。</returns>
    Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 获取全部启用配置。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>同步表定义列表。</returns>
    Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct);

    /// <summary>
    /// 获取全局多表并发上限。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>并发上限。</returns>
    Task<int> GetMaxParallelTablesAsync(CancellationToken ct);
}
