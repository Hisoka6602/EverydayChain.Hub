using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 自动迁移服务测试桩。
/// </summary>
public sealed class TestAutoMigrationService : IAutoMigrationService
{
    /// <summary>
    /// 运行次数。
    /// </summary>
    public int RunCount { get; private set; }

    /// <summary>
    /// 运行时待抛出的异常；为空时表示正常完成。
    /// </summary>
    public Exception? ExceptionToThrow { get; init; }

    /// <inheritdoc />
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
