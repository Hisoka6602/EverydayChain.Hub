namespace EverydayChain.Hub.Infrastructure.Sync.Abstractions;

/// <summary>
/// SQL Server 仅追加写入抽象。
/// </summary>
public interface ISqlServerAppendOnlyWriter
{
    /// <summary>
    /// 批量追加数据到目标分表。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="targetLogicalTable">目标逻辑表。</param>
    /// <param name="rows">待写入行集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>成功追加行数。</returns>
    Task<int> AppendAsync(
        string tableCode,
        string targetLogicalTable,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct);
}
