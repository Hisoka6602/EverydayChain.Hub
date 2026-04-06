namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 分表解析仓储接口。
/// </summary>
public interface IShardTableResolver
{
    /// <summary>
    /// 列出指定逻辑表对应的全部物理分表名。
    /// </summary>
    /// <param name="logicalTableName">逻辑表名。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>物理分表名集合。</returns>
    Task<IReadOnlyList<string>> ListPhysicalTablesAsync(string logicalTableName, CancellationToken ct);

    /// <summary>
    /// 解析物理分表名对应的月份标识。
    /// </summary>
    /// <param name="physicalTableName">物理分表名。</param>
    /// <returns>解析成功返回月份时间，否则返回空。</returns>
    DateTime? TryParseShardMonth(string physicalTableName);
}
