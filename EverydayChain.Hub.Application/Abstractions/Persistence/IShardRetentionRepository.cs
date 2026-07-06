namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IShardRetentionRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<string> GenerateRollbackScriptAsync(string logicalTableName, string physicalTableName, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task DropShardTableAsync(string logicalTableName, string physicalTableName, string rollbackScript, CancellationToken ct);
}

