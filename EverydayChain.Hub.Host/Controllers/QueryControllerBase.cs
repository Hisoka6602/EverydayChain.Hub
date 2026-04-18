using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 查询控制器基类，统一处理“请求体为空但查询字符串有值”场景，避免触发默认模型绑定的 request 必填错误。
/// 继承方应同时使用 <c>[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]</c> 与 <c>[FromQuery]</c> 进行双来源绑定。
/// </summary>
public abstract class QueryControllerBase : ControllerBase
{
    /// <summary>
    /// 解析查询请求来源，优先级为：请求体 > 查询字符串 > 默认实例。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <param name="request">请求体请求。</param>
    /// <param name="queryRequest">查询字符串请求。</param>
    /// <returns>解析后的请求对象。</returns>
    protected static TRequest ResolveRequest<TRequest>(TRequest? request, TRequest? queryRequest)
        where TRequest : class, new()
    {
        return request ?? queryRequest ?? new TRequest();
    }
}
