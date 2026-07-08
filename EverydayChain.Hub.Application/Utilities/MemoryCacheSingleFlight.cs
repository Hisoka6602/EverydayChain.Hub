using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Application.Utilities;

/// <summary>
/// 定义 MemoryCacheSingleFlight 类型。
/// </summary>
public static class MemoryCacheSingleFlight
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<object?>>> InflightTasks = new(StringComparer.Ordinal);

    /// <summary>
    /// 执行当前业务方法。
    /// </summary>
    public static async Task<T> GetOrCreateAsync<T>(
        IMemoryCache memoryCache,
        string cacheKey,
        TimeSpan timeToLive,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        // 步骤：执行当前业务方法的核心处理流程。
        if (memoryCache.TryGetValue(cacheKey, out object? cachedValue))
        {
            return cachedValue is null ? default! : (T)cachedValue;
        }

        Lazy<Task<object?>>? lazyTask = null;
        lazyTask = new Lazy<Task<object?>>(
            () => PopulateCacheAsync(memoryCache, cacheKey, timeToLive, factory, lazyTask),
            LazyThreadSafetyMode.ExecutionAndPublication);
        var sharedTask = InflightTasks.GetOrAdd(cacheKey, lazyTask);
        var result = await sharedTask.Value.WaitAsync(cancellationToken);
        return result is null ? default! : (T)result;
    }

    /// <summary>
    /// 执行当前业务方法。
    /// </summary>
    private static async Task<object?> PopulateCacheAsync<T>(
        IMemoryCache memoryCache,
        string cacheKey,
        TimeSpan timeToLive,
        Func<CancellationToken, Task<T>> factory,
        Lazy<Task<object?>>? lazyTask)
    {
        // 步骤：执行当前业务方法的核心处理流程。
        try
        {
            if (memoryCache.TryGetValue(cacheKey, out object? cachedValue))
            {
                return cachedValue;
            }

            var value = await factory(CancellationToken.None);
            memoryCache.Set(cacheKey, value, timeToLive);
            return value;
        }
        finally
        {
            if (lazyTask is not null)
            {
                InflightTasks.TryRemove(new KeyValuePair<string, Lazy<Task<object?>>>(cacheKey, lazyTask));
            }
        }
    }
}

