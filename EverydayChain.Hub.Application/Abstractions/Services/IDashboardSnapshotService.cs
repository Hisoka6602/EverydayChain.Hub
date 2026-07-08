namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IDashboardSnapshotService 类型。
/// </summary>
public interface IDashboardSnapshotService
{
    /// <summary>
    /// 执行 RefreshAsync 方法。
    /// </summary>
    Task RefreshAsync(CancellationToken ct);
}

