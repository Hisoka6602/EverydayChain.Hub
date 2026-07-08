namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 NonRetryableDangerZoneException 类型。
/// </summary>
public sealed class NonRetryableDangerZoneException(string message, Exception? innerException = null)
    : Exception(message, innerException);

