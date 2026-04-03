namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 运行期存储守护服务接口。
/// </summary>
public interface IRuntimeStorageGuard
{
    /// <summary>
    /// 执行启动阶段存储健康自检。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    Task EnsureStartupHealthyAsync(CancellationToken ct);

    /// <summary>
    /// 在关键写入前校验可用磁盘空间。
    /// </summary>
    /// <param name="targetPath">目标文件路径。</param>
    /// <param name="scene">业务场景说明。</param>
    /// <param name="ct">取消令牌。</param>
    Task EnsureWriteSpaceAsync(string targetPath, string scene, CancellationToken ct);
}
