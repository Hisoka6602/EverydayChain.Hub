using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EverydayChain.Hub.Host.Startup;

/// <summary>
/// 注册运行期韧性保护配置。
/// </summary>
public static class RuntimeResilienceServiceCollectionExtensions
{
    /// <summary>
    /// 添加避免后台任务异常停止宿主的运行期保护。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddRuntimeResilienceGuards(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Configure<HostOptions>(options =>
        {
            // 步骤：后台服务发生未捕获异常时仅让对应任务结束，避免触发整个宿主停止。
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });

        return services;
    }
}
