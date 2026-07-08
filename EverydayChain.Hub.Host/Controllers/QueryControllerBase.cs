using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 定义查询类控制器公共基类，负责统一解析请求体与查询字符串中的查询参数。
/// </summary>
public abstract class QueryControllerBase : ControllerBase
{
    /// <summary>
    /// 解析最终使用的请求对象。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <param name="request">请求体对象。</param>
    /// <param name="queryRequest">查询字符串对象。</param>
    /// <returns>优先返回请求体，其次返回查询字符串对象，最后返回默认实例。</returns>
    protected static TRequest ResolveRequest<TRequest>(TRequest? request, TRequest? queryRequest)
        where TRequest : class, new()
    {
        return request ?? queryRequest ?? new TRequest();
    }
}

