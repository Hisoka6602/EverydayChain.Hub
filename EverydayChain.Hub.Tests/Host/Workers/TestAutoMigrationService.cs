using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class TestAutoMigrationService : IAutoMigrationService
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RunCount { get; private set; }

    /// <summary>
    /// 获取或设置当前属性值。
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

