namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IShardTableResolver
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<string>> ListPhysicalTablesAsync(string logicalTableName, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    DateTime? TryParseShardMonth(string physicalTableName);
}

