using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义保留期清理审计查询服务契约。
/// 该服务用于向运维页面返回自动保留期清理任务的分页留痕结果。
/// </summary>
public interface IRetentionCleanupQueryService
{
    /// <summary>
    /// 查询保留期清理审计记录。
    /// </summary>
    /// <param name="request">查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页查询结果。</returns>
    Task<RetentionCleanupAuditQueryResult> QueryAsync(RetentionCleanupAuditQueryRequest request, CancellationToken cancellationToken);
}
