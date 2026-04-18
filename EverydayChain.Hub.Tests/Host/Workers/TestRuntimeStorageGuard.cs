using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 运行期存储守护测试桩。
/// </summary>
public sealed class TestRuntimeStorageGuard : IRuntimeStorageGuard
{
    /// <summary>
    /// 启动健康检查调用次数。
    /// </summary>
    public int StartupHealthCheckCount { get; private set; }

    /// <summary>
    /// 启动健康检查待抛出的异常；为空时表示校验通过。
    /// </summary>
    public Exception? StartupExceptionToThrow { get; init; }

    /// <inheritdoc />
    public Task EnsureStartupHealthyAsync(CancellationToken ct)
    {
        StartupHealthCheckCount++;
        if (StartupExceptionToThrow is not null)
        {
            throw StartupExceptionToThrow;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EnsureWriteSpaceAsync(string targetPath, string scene, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportTableMemoryAsync(string tableCode, int entryCount, string scene, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
