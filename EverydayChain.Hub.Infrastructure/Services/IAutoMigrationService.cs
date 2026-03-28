namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 自动迁移服务接口，负责在应用启动时执行基础迁移并预创建分表。
/// </summary>
public interface IAutoMigrationService
{
    /// <summary>
    /// 执行基础迁移，并预创建当前及未来若干月的分表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task RunAsync(CancellationToken cancellationToken);
}
