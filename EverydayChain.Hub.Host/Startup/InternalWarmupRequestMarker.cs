using Microsoft.AspNetCore.Http;

namespace EverydayChain.Hub.Host.Startup;

/// <summary>
/// 标记内部启动预热请求，便于中间件识别并避免污染普通失败日志。
/// </summary>
public static class InternalWarmupRequestMarker
{
    /// <summary>
    /// 预热请求标记头名称。
    /// </summary>
    public const string HeaderName = "X-Internal-Warmup";

    /// <summary>
    /// 预热请求标记头值。
    /// </summary>
    public const string HeaderValue = "true";

    /// <summary>
    /// 判断当前请求是否为内部预热请求。
    /// </summary>
    /// <param name="request">HTTP 请求。</param>
    /// <returns>若为内部预热请求则返回 <c>true</c>。</returns>
    public static bool IsMarked(HttpRequest request)
    {
        return string.Equals(
            request.Headers[HeaderName].ToString().Trim(),
            HeaderValue,
            StringComparison.OrdinalIgnoreCase);
    }
}
