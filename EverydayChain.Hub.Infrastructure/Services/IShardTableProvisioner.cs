namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 分表预置服务接口，负责在目标数据库中按需创建分表结构。
/// </summary>
public interface IShardTableProvisioner
{
    /// <summary>
    /// 确保单个分表存在；若不存在则自动创建。
    /// </summary>
    /// <param name="suffix">分表后缀，例如 <c>_202603</c>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken);

    /// <summary>
    /// 批量确保多个分表存在；并发执行，全部完成后返回。
    /// </summary>
    /// <param name="suffixes">分表后缀集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken);
}
