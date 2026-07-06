namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IAutoMigrationService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}

