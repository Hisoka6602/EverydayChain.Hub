using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Tests.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ApiWarmupHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenWarmupServiceThrows()
    {
        var hostedService = CreateHostedService(new ThrowingApiWarmupService());

        await hostedService.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_ShouldInvokeWarmupService()
    {
        var warmupService = new RecordingApiWarmupService();
        var hostedService = CreateHostedService(warmupService);

        await hostedService.StartAsync(CancellationToken.None);
        var isCompleted = await warmupService.InvocationCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(isCompleted);
        Assert.Equal(1, warmupService.InvocationCount);
    }

    private static ApiWarmupHostedService CreateHostedService(IApiWarmupService apiWarmupService)
    {
        var dbContextFactory = new ThrowingHubDbContextFactory();
        IShardSuffixResolver shardSuffixResolver = new FixedBootstrapShardSuffixResolver(["_202604"]);

        return new ApiWarmupHostedService(
            apiWarmupService,
            dbContextFactory,
            shardSuffixResolver,
            new TestHostApplicationLifetime(),
            NullLogger<ApiWarmupHostedService>.Instance);
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class RecordingApiWarmupService : IApiWarmupService
    {
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public int InvocationCount { get; private set; }

        public TaskCompletionSource<bool> InvocationCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WarmupAsync(CancellationToken cancellationToken)
        {
            InvocationCount++;
            InvocationCompletion.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class ThrowingApiWarmupService : IApiWarmupService
    {
        public Task WarmupAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("测试桩：预热失败。");
        }
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public CancellationToken ApplicationStarted => CancellationToken.None;

        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public CancellationToken ApplicationStopping => CancellationToken.None;

        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }
}

