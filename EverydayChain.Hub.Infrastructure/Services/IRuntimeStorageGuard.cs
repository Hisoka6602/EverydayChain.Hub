namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IRuntimeStorageGuard
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task EnsureStartupHealthyAsync(CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task EnsureWriteSpaceAsync(string targetPath, string scene, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task ReportTableMemoryAsync(string tableCode, int entryCount, string scene, CancellationToken ct);
}

