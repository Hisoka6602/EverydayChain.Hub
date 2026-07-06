using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Tests.SharedKernel;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class MemoryCacheSingleFlightTests
{
    [Fact]
    public async Task GetOrCreateAsync_ShouldCoalesceConcurrentFactoryExecution()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var executionCount = 0;
        var results = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => MemoryCacheSingleFlight.GetOrCreateAsync(
                memoryCache,
                "key-1",
                TimeSpan.FromSeconds(10),
                /// <summary>
                /// 获取或设置当前属性值。
                /// </summary>
                async _ =>
                {
                    Interlocked.Increment(ref executionCount);
                    await Task.Delay(50);
                    return "value-1";
                },
                CancellationToken.None))
            .ToArray();

        foreach (var item in await Task.WhenAll(tasks))
        {
            results.Add(item);
        }

        Assert.Equal(1, executionCount);
        Assert.Equal(10, results.Count);
        Assert.All(results, item => Assert.Equal("value-1", item));
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldCacheNullResults()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var executionCount = 0;

        var first = await MemoryCacheSingleFlight.GetOrCreateAsync<string?>(
            memoryCache,
            "key-2",
            TimeSpan.FromSeconds(10),
            /// <summary>
            /// 获取或设置当前属性值。
            /// </summary>
            _ =>
            {
                Interlocked.Increment(ref executionCount);
                return Task.FromResult<string?>(null);
            },
            CancellationToken.None);
        var second = await MemoryCacheSingleFlight.GetOrCreateAsync<string?>(
            memoryCache,
            "key-2",
            TimeSpan.FromSeconds(10),
            /// <summary>
            /// 获取或设置当前属性值。
            /// </summary>
            _ =>
            {
                Interlocked.Increment(ref executionCount);
                return Task.FromResult<string?>(null);
            },
            CancellationToken.None);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, executionCount);
    }
}

