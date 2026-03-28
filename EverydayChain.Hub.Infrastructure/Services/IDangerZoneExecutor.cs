namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 危险操作隔离器接口，为 DDL、批量删除、批量更新等高风险操作提供超时、重试与熔断保护。
/// </summary>
public interface IDangerZoneExecutor
{
    /// <summary>
    /// 在隔离策略保护下执行无返回值的异步操作。
    /// </summary>
    /// <param name="operationName">操作名称，用于日志区分。</param>
    /// <param name="action">待执行的异步委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task ExecuteAsync(string operationName, Func<CancellationToken, Task> action, CancellationToken cancellationToken);

    /// <summary>
    /// 在隔离策略保护下执行有返回值的异步操作。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="operationName">操作名称，用于日志区分。</param>
    /// <param name="action">待执行的异步委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作执行结果。</returns>
    Task<T> ExecuteAsync<T>(string operationName, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
