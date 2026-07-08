namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 IShardTableProvisioner 类型。
/// </summary>
public interface IShardTableProvisioner
{
    /// <summary>
    /// 执行 EnsureShardTableAsync 方法。
    /// </summary>
    Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 EnsureShardTableAsync 方法。
    /// </summary>
    Task EnsureShardTableAsync(string logicalTable, string suffix, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 EnsureShardTablesAsync 方法。
    /// </summary>
    Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken);
}

