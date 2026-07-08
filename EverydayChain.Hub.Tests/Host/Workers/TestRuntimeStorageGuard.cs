using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 定义 TestRuntimeStorageGuard 类型。
/// </summary>
public sealed class TestRuntimeStorageGuard : IRuntimeStorageGuard
{
    /// <summary>
    /// 获取或设置 StartupHealthCheckCount。
    /// </summary>
    public int StartupHealthCheckCount { get; private set; }

    /// <summary>
    /// 获取或设置 StartupExceptionToThrow。
    /// </summary>
    public Exception? StartupExceptionToThrow { get; init; }

    public Task EnsureStartupHealthyAsync(CancellationToken ct)
    {
        StartupHealthCheckCount++;
        if (StartupExceptionToThrow is not null)
        {
            throw StartupExceptionToThrow;
        }

        return Task.CompletedTask;
    }

    public Task EnsureWriteSpaceAsync(string targetPath, string scene, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task ReportTableMemoryAsync(string tableCode, int entryCount, string scene, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

