using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 TestLogger 类型。
/// </summary>
public sealed class TestLogger<T> : ILogger<T>
{
    /// <summary>
    /// 获取或设置 Logs。
    /// </summary>
    public List<LogEntry> Logs { get; } = [];

    /// <summary>
    /// 执行当前业务方法。
    /// </summary>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        // 步骤：执行当前业务方法的核心处理流程。
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
    /// 定义 LogEntry 类型。
    /// </summary>
    public readonly record struct LogEntry(LogLevel Level, string Message);

}


