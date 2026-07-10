namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 为分钟级快照查询规划可复用的快照区间与边界回源区间。
/// </summary>
public static class SnapshotQueryPlanBuilder
{
    /// <summary>
    /// 基于请求区间与快照覆盖区间构建查询计划。
    /// </summary>
    /// <param name="requestStartLocal">请求开始时间。</param>
    /// <param name="requestEndLocal">请求结束时间。</param>
    /// <param name="coverageStartLocal">快照覆盖开始时间。</param>
    /// <param name="coverageEndLocal">快照覆盖结束时间。</param>
    /// <returns>可复用的快照查询计划；不存在可用快照核心区间时返回 <c>null</c>。</returns>
    public static SnapshotQueryPlan? Build(
        DateTime requestStartLocal,
        DateTime requestEndLocal,
        DateTime? coverageStartLocal,
        DateTime? coverageEndLocal)
    {
        if (requestEndLocal <= requestStartLocal
            || !coverageStartLocal.HasValue
            || !coverageEndLocal.HasValue)
        {
            return null;
        }

        var requestSnapshotStartLocal = AlignUpToMinute(requestStartLocal);
        var requestSnapshotEndLocal = AlignDownToMinute(requestEndLocal);
        var coveredSnapshotStartLocal = AlignUpToMinute(coverageStartLocal.Value);
        var coveredSnapshotEndLocal = AlignDownToMinute(coverageEndLocal.Value);
        var snapshotStartLocal = requestSnapshotStartLocal >= coveredSnapshotStartLocal
            ? requestSnapshotStartLocal
            : coveredSnapshotStartLocal;
        var snapshotEndLocal = requestSnapshotEndLocal <= coveredSnapshotEndLocal
            ? requestSnapshotEndLocal
            : coveredSnapshotEndLocal;
        if (snapshotStartLocal >= snapshotEndLocal)
        {
            return null;
        }

        var baseTableRanges = new List<SnapshotQueryRange>(2);
        if (requestStartLocal < snapshotStartLocal)
        {
            baseTableRanges.Add(new SnapshotQueryRange(requestStartLocal, snapshotStartLocal));
        }

        if (snapshotEndLocal < requestEndLocal)
        {
            baseTableRanges.Add(new SnapshotQueryRange(snapshotEndLocal, requestEndLocal));
        }

        return new SnapshotQueryPlan(
            new SnapshotQueryRange(snapshotStartLocal, snapshotEndLocal),
            baseTableRanges);
    }

    /// <summary>
    /// 判断时间是否已经按整分钟对齐。
    /// </summary>
    /// <param name="value">待判断时间。</param>
    /// <returns>按整分钟对齐时返回真。</returns>
    public static bool IsMinuteAligned(DateTime value)
    {
        return value.Second == 0
            && value.Millisecond == 0
            && value.Ticks % TimeSpan.TicksPerSecond == 0;
    }

    private static DateTime AlignUpToMinute(DateTime value)
    {
        if (IsMinuteAligned(value))
        {
            return value;
        }

        return AlignDownToMinute(value).AddMinutes(1);
    }

    private static DateTime AlignDownToMinute(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
    }
}

/// <summary>
/// 定义快照查询计划。
/// </summary>
/// <param name="SnapshotRange">可直接走快照的整分钟核心区间。</param>
/// <param name="BaseTableRanges">首尾需回源业务表的边界区间集合。</param>
public sealed record SnapshotQueryPlan(
    SnapshotQueryRange SnapshotRange,
    IReadOnlyList<SnapshotQueryRange> BaseTableRanges);

/// <summary>
/// 定义快照查询中的时间区间。
/// </summary>
/// <param name="StartLocal">开始时间。</param>
/// <param name="EndLocal">结束时间。</param>
public readonly record struct SnapshotQueryRange(DateTime StartLocal, DateTime EndLocal);
