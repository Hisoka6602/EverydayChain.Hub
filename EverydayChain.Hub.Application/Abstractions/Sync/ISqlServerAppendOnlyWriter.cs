namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// SQL Server 仅追加写入抽象，表达向 SQL Server 目标分表批量追加数据的外部协作能力。
/// </summary>
public interface ISqlServerAppendOnlyWriter
{
    /// <summary>
    /// 批量追加数据到目标分表（幂等：当 <paramref name="uniqueKeys"/> 非空时，已存在的行将被跳过）。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="targetLogicalTable">目标逻辑表。</param>
    /// <param name="rows">待写入行集合。</param>
    /// <param name="uniqueKeys">
    /// 业务唯一键列名集合，用于去重判断。非空时使用暂存表加条件插入，
    /// 跳过目标表中已存在相同唯一键的行，确保追加操作幂等；空集合则退化为直接 BulkCopy。
    /// </param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>成功追加（实际插入）行数。</returns>
    Task<int> AppendAsync(
        string tableCode,
        string targetLogicalTable,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<string> uniqueKeys,
        CancellationToken ct);
}
