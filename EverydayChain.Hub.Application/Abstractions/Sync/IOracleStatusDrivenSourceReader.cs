using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// Oracle 状态驱动源读取抽象，表达从 Oracle 源端按状态列分页读取待处理数据的外部协作能力。
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
    /// <param name="window">
    /// 同步时间窗口（可为 default）。当 <see cref="SyncTableDefinition.CursorColumn"/> 非空时，
    /// 追加游标列时间范围条件 <c>CursorColumn &gt;= WindowStart AND CursorColumn &lt;= WindowEnd</c>，
    /// 以避免全表状态扫描。CursorColumn 为空时此参数被忽略。
    /// </param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>数据行集合（包含 __RowId）。</returns>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadPendingPageAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        int pageNo,
        int pageSize,
        IReadOnlySet<string> normalizedExcludedColumns,
        SyncWindow window,
        CancellationToken ct);
}
