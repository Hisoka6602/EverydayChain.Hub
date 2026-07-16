using EverydayChain.Hub.Host.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Host.Startup;

/// <summary>
/// 验证运行期韧性服务注册。
/// </summary>
public sealed class RuntimeResilienceServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证后台服务异常不会配置为停止宿主。
    /// </summary>
    [Fact]
    public void AddRuntimeResilienceGuards_ShouldIgnoreBackgroundServiceExceptions()
    {
        var services = new ServiceCollection();

        services.AddRuntimeResilienceGuards();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HostOptions>>().Value;
        Assert.Equal(BackgroundServiceExceptionBehavior.Ignore, options.BackgroundServiceExceptionBehavior);
    }
}
