using System.Collections.Concurrent;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义 BoundedConcurrentQueueHelper 类型。
/// </summary>
public static class BoundedConcurrentQueueHelper
{
    /// <summary>
    /// 执行当前业务方法。
    /// </summary>
    public static void TrimExcessIfNeeded<TItem>(
        ConcurrentQueue<TItem> queue,
        int maxCapacity,
        int extraEvictionCount)
    {
        // 步骤：执行当前业务方法的核心处理流程。
        var currentCount = queue.Count;
        if (currentCount <= maxCapacity)
        {
            return;
        }

        var evictionCount = currentCount - maxCapacity + extraEvictionCount;
        for (var i = 0; i < evictionCount; i++)
        {
            queue.TryDequeue(out _);
        }
    }
}
