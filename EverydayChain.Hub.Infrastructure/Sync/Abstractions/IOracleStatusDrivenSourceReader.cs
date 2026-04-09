using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Infrastructure.Sync.Abstractions;

/// <summary>
/// Oracle 状态驱动源读取抽象。
/// </summary>
public interface IOracleStatusDrivenSourceReader
{
    /// <summary>
    /// 读取待处理状态页。
    /// </summary>
    /// <param name="definition">同步表定义。</param>
    /// <param name="profile">状态消费配置。</param>
    /// <param name="pageNo">页码（从 1 开始）。</param>
    /// <param name="pageSize">分页大小。</param>
    /// <param name="normalizedExcludedColumns">规范化排除列集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>数据行集合（包含 __RowId）。</returns>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadPendingPageAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        int pageNo,
        int pageSize,
        IReadOnlySet<string> normalizedExcludedColumns,
        CancellationToken ct);
}
