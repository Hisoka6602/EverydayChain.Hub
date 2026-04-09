using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 测试日志记录器。
/// </summary>
/// <typeparam name="T">日志分类类型。</typeparam>
public sealed class TestLogger<T> : ILogger<T>
{
    /// <summary>日志集合。</summary>
    public List<LogEntry> Logs { get; } = [];

    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return default(Scope);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Logs.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    /// <summary>
    /// 日志条目。
    /// </summary>
    /// <param name="Level">日志级别。</param>
    /// <param name="Message">日志内容。</param>
    public readonly record struct LogEntry(LogLevel Level, string Message);

    /// <summary>
    /// 空作用域结构体。
    /// </summary>
    private readonly struct Scope : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
