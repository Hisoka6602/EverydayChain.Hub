namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 标记为“不可重试”的危险操作异常。
/// 用于告知隔离器：该错误为确定性失败（如配置错误），无需指数退避重试。
/// </summary>
public sealed class NonRetryableDangerZoneException(string message, Exception? innerException = null)
    : Exception(message, innerException);
