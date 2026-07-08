namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 IAutoMigrationService 类型。
/// </summary>
public interface IAutoMigrationService
{
    /// <summary>
    /// 执行 RunAsync 方法。
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}

