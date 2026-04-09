namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 测试日志空作用域单例。
/// </summary>
public sealed class LoggerNullScope : IDisposable
{
    /// <summary>空作用域单例。</summary>
    public static readonly LoggerNullScope Instance = new();

    /// <summary>
    /// 私有构造方法，禁止外部实例化。
    /// </summary>
    private LoggerNullScope()
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
