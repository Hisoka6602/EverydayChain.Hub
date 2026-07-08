namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 IShardTableResolver 类型。
/// </summary>
public interface IShardTableResolver
{
    /// <summary>
    /// 执行 ListPhysicalTablesAsync 方法。
    /// </summary>
    Task<IReadOnlyList<string>> ListPhysicalTablesAsync(string logicalTableName, CancellationToken ct);

    /// <summary>
    /// 执行 TryParseShardMonth 方法。
    /// </summary>
    DateTime? TryParseShardMonth(string physicalTableName);
}

