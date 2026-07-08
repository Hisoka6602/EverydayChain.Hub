using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 ISyncTaskConfigRepository 类型。
/// </summary>
public interface ISyncTaskConfigRepository
{
    /// <summary>
    /// 执行 GetByTableCodeAsync 方法。
    /// </summary>
    Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 执行 ListEnabledAsync 方法。
    /// </summary>
    Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct);

    /// <summary>
    /// 执行 GetMaxParallelTablesAsync 方法。
    /// </summary>
    Task<int> GetMaxParallelTablesAsync(CancellationToken ct);
}

