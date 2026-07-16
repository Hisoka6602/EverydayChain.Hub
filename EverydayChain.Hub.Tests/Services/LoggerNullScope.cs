namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 LoggerNullScope 类型。
/// </summary>
public sealed class LoggerNullScope : IDisposable
{
    /// <summary>
    /// 提供测试日志作用域的共享空实例。
    /// </summary>
    public static readonly LoggerNullScope Instance = new();

    private LoggerNullScope()
    {
    }

    public void Dispose()
    {
    }
}

