namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 LoggerNullScope 类型。
/// </summary>
public sealed class LoggerNullScope : IDisposable
{
    public static readonly LoggerNullScope Instance = new();

    private LoggerNullScope()
    {
    }

    public void Dispose()
    {
    }
}

