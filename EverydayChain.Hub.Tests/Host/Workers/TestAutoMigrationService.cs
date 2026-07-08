using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 定义 TestAutoMigrationService 类型。
/// </summary>
public sealed class TestAutoMigrationService : IAutoMigrationService
{
    /// <summary>
    /// 获取或设置 RunCount。
    /// </summary>
    public int RunCount { get; private set; }

    /// <summary>
    /// 获取或设置 ExceptionToThrow。
    /// </summary>
    public Exception? ExceptionToThrow { get; init; }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        RunCount++;
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.CompletedTask;
    }
}

