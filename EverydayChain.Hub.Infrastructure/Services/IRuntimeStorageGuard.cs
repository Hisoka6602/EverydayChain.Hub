namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 IRuntimeStorageGuard 类型。
/// </summary>
public interface IRuntimeStorageGuard
{
    /// <summary>
    /// 执行 EnsureStartupHealthyAsync 方法。
    /// </summary>
    Task EnsureStartupHealthyAsync(CancellationToken ct);

    /// <summary>
    /// 执行 EnsureWriteSpaceAsync 方法。
    /// </summary>
    Task EnsureWriteSpaceAsync(string targetPath, string scene, CancellationToken ct);

    /// <summary>
    /// 执行 ReportTableMemoryAsync 方法。
    /// </summary>
    Task ReportTableMemoryAsync(string tableCode, int entryCount, string scene, CancellationToken ct);
}

