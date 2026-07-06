using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class TestLogger<T> : ILogger<T>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<LogEntry> Logs { get; } = [];

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        // 步骤：按既定流程执行当前方法逻辑。
        return LoggerNullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Logs.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    public readonly record struct LogEntry(LogLevel Level, string Message);

}


