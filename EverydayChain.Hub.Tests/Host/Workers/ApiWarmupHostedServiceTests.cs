using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Tests.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// API 启动预热托管服务测试。
/// </summary>
public sealed class ApiWarmupHostedServiceTests
{
    /// <summary>
    /// 预热服务抛出异常时不应影响宿主启动。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenWarmupServiceThrows()
    {
        var hostedService = CreateHostedService(new ThrowingApiWarmupService());

        await hostedService.StartAsync(CancellationToken.None);
    }

    /// <summary>
    /// 启动后应异步触发一次预热执行。
    /// </summary>
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

    /// <summary>
    /// 创建测试用托管服务实例。
    /// </summary>
    /// <param name="apiWarmupService">预热服务。</param>
    /// <returns>托管服务实例。</returns>
    private static ApiWarmupHostedService CreateHostedService(IApiWarmupService apiWarmupService)
    {
        var dbContextFactory = new ThrowingHubDbContextFactory();
        IShardSuffixResolver shardSuffixResolver = new FixedBootstrapShardSuffixResolver(["_202604"]);

        return new ApiWarmupHostedService(
            apiWarmupService,
            dbContextFactory,
            shardSuffixResolver,
            NullLogger<ApiWarmupHostedService>.Instance);
    }

    /// <summary>
    /// 记录调用次数的预热服务替身。
    /// </summary>
    private sealed class RecordingApiWarmupService : IApiWarmupService
    {
        /// <summary>
        /// 调用计数。
        /// </summary>
        public int InvocationCount { get; private set; }

        /// <summary>
        /// 调用完成通知。
        /// </summary>
        public TaskCompletionSource<bool> InvocationCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc/>
        public Task WarmupAsync(CancellationToken cancellationToken)
        {
            InvocationCount++;
            InvocationCompletion.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 抛出异常的预热服务替身。
    /// </summary>
    private sealed class ThrowingApiWarmupService : IApiWarmupService
    {
        /// <inheritdoc/>
        public Task WarmupAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("测试桩：预热失败。");
        }
    }
}
