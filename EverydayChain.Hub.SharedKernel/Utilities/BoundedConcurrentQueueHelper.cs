using System.Collections.Concurrent;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 有界并发队列淘汰辅助工具，提供线程安全的队列容量保护能力。
/// </summary>
public static class BoundedConcurrentQueueHelper
{
    /// <summary>
    /// 当队列条目超过 <paramref name="maxCapacity"/> 时，淘汰最早入队的条目，防止无界增长。
    /// <para>仅对 <c>Count</c> 执行一次 O(n) 遍历并缓存结果，避免在判断与计算中重复遍历。</para>
    /// </summary>
    /// <typeparam name="TItem">队列条目类型。</typeparam>
    /// <param name="queue">待裁剪的并发队列。</param>
    /// <param name="maxCapacity">允许保留的最大条目数（水位上限）。</param>
    /// <param name="extraEvictionCount">
    /// 超限后额外多移除的条目数，使水位回落至上限以下，
    /// 降低后续写入频繁触发淘汰的概率。实际单次移除数为 <c>currentCount - maxCapacity + extraEvictionCount</c>。
    /// </param>
    public static void TrimExcessIfNeeded<TItem>(
        ConcurrentQueue<TItem> queue,
        int maxCapacity,
        int extraEvictionCount)
    {
        // 缓存 Count 结果，避免对 ConcurrentQueue 执行两次 O(n) 遍历。
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
