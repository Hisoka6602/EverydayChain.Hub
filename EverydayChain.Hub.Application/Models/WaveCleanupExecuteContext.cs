namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 表示波次清理正式执行时需要一并记录的请求上下文。
/// 该模型只用于审计，不参与任何分拣机业务逻辑计算。
/// </summary>
public sealed class WaveCleanupExecuteContext
{
    /// <summary>
    /// 获取或设置请求链路跟踪标识。
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置请求路径。
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置请求方法。
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置调用方自报的操作人标识。
    /// </summary>
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置客户端 IP 地址。
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置客户端 User-Agent。
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;
}
