using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Host.Startup;

/// <summary>
/// 绑定进程级异常兜底处理。
/// </summary>
public static class RuntimeExceptionGuard
{
    /// <summary>
    /// 存储进程级异常守卫是否已经注册。
    /// </summary>
    private static int _isRegistered;

    /// <summary>
    /// 注册进程级异常兜底处理。
    /// </summary>
    /// <param name="logger">异常日志记录器。</param>
    public static void Register(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (Interlocked.Exchange(ref _isRegistered, 1) == 1)
        {
            return;
        }

        // 步骤：处理未观察的异步任务异常，避免终结器线程升级为进程级风险。
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            TryLog(
                logger,
                LogLevel.Error,
                args.Exception,
                "检测到未观察的异步任务异常，已标记为已观察，避免影响长期运行。");
            args.SetObserved();
        };

        // 步骤：记录进程级未处理异常，给无人值守场景留下最后的诊断信息。
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            TryLog(
                logger,
                LogLevel.Critical,
                exception,
                "检测到进程级未处理异常，运行时可能仍会按公共语言运行时规则终止进程。");
        };
    }

    /// <summary>
    /// 尝试写入异常日志，并确保日志通道自身异常不会外溢。
    /// </summary>
    /// <param name="logger">异常日志记录器。</param>
    /// <param name="level">日志等级。</param>
    /// <param name="exception">异常对象。</param>
    /// <param name="message">日志消息。</param>
    private static void TryLog(ILogger logger, LogLevel level, Exception? exception, string message)
    {
        try
        {
            if (exception is null)
            {
                logger.Log(level, "{Message}", message);
                return;
            }

            logger.Log(level, exception, "{Message}", message);
        }
        catch
        {
        }
    }
}
