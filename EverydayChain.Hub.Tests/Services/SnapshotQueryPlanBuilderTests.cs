using EverydayChain.Hub.Infrastructure.Repositories;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 SnapshotQueryPlanBuilderTests 类型。
/// </summary>
public sealed class SnapshotQueryPlanBuilderTests
{
    [Fact]
    public void Build_ShouldReturnNull_WhenRequestRangeDoesNotContainFullMinuteCore()
    {
        var requestStartLocal = new DateTime(2026, 7, 10, 12, 0, 10, DateTimeKind.Local);
        var requestEndLocal = new DateTime(2026, 7, 10, 12, 0, 50, DateTimeKind.Local);

        var plan = SnapshotQueryPlanBuilder.Build(
            requestStartLocal,
            requestEndLocal,
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Local),
            new DateTime(2026, 7, 10, 12, 5, 0, DateTimeKind.Local));

        Assert.Null(plan);
    }

    [Fact]
    public void Build_ShouldReturnSnapshotOnlyPlan_WhenRequestRangeIsMinuteAlignedAndCovered()
    {
        var requestStartLocal = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Local);
        var requestEndLocal = new DateTime(2026, 7, 10, 12, 5, 0, DateTimeKind.Local);

        var plan = SnapshotQueryPlanBuilder.Build(
            requestStartLocal,
            requestEndLocal,
            new DateTime(2026, 7, 10, 11, 0, 0, DateTimeKind.Local),
            new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Local));

        Assert.NotNull(plan);
        Assert.Equal(requestStartLocal, plan!.SnapshotRange.StartLocal);
        Assert.Equal(requestEndLocal, plan.SnapshotRange.EndLocal);
        Assert.Empty(plan.BaseTableRanges);
    }

    [Fact]
    public void Build_ShouldReturnHybridPlan_WhenOnlyMiddleAlignedRangeIsCovered()
    {
        var requestStartLocal = new DateTime(2026, 7, 10, 12, 0, 10, DateTimeKind.Local);
        var requestEndLocal = new DateTime(2026, 7, 10, 12, 3, 20, DateTimeKind.Local);

        var plan = SnapshotQueryPlanBuilder.Build(
            requestStartLocal,
            requestEndLocal,
            new DateTime(2026, 7, 10, 12, 1, 0, DateTimeKind.Local),
            new DateTime(2026, 7, 10, 12, 3, 0, DateTimeKind.Local));

        Assert.NotNull(plan);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 1, 0, DateTimeKind.Local), plan!.SnapshotRange.StartLocal);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 3, 0, DateTimeKind.Local), plan.SnapshotRange.EndLocal);
        Assert.Equal(2, plan.BaseTableRanges.Count);
        Assert.Equal(requestStartLocal, plan.BaseTableRanges[0].StartLocal);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 1, 0, DateTimeKind.Local), plan.BaseTableRanges[0].EndLocal);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 3, 0, DateTimeKind.Local), plan.BaseTableRanges[1].StartLocal);
        Assert.Equal(requestEndLocal, plan.BaseTableRanges[1].EndLocal);
    }

    [Fact]
    public void Build_ShouldReturnHybridPlan_WhenSnapshotCoverageOnlyCoversMiddleOverlap()
    {
        var requestStartLocal = new DateTime(2026, 7, 10, 12, 0, 10, DateTimeKind.Local);
        var requestEndLocal = new DateTime(2026, 7, 10, 12, 3, 20, DateTimeKind.Local);

        var plan = SnapshotQueryPlanBuilder.Build(
            requestStartLocal,
            requestEndLocal,
            new DateTime(2026, 7, 10, 12, 1, 0, DateTimeKind.Local),
            new DateTime(2026, 7, 10, 12, 2, 0, DateTimeKind.Local));

        Assert.NotNull(plan);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 1, 0, DateTimeKind.Local), plan!.SnapshotRange.StartLocal);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 2, 0, DateTimeKind.Local), plan.SnapshotRange.EndLocal);
        Assert.Equal(2, plan.BaseTableRanges.Count);
        Assert.Equal(requestStartLocal, plan.BaseTableRanges[0].StartLocal);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 1, 0, DateTimeKind.Local), plan.BaseTableRanges[0].EndLocal);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 2, 0, DateTimeKind.Local), plan.BaseTableRanges[1].StartLocal);
        Assert.Equal(requestEndLocal, plan.BaseTableRanges[1].EndLocal);
    }

    [Fact]
    public void Build_ShouldReturnNull_WhenSnapshotCoverageHasNoFullMinuteOverlap()
    {
        var requestStartLocal = new DateTime(2026, 7, 10, 12, 0, 10, DateTimeKind.Local);
        var requestEndLocal = new DateTime(2026, 7, 10, 12, 3, 20, DateTimeKind.Local);

        var plan = SnapshotQueryPlanBuilder.Build(
            requestStartLocal,
            requestEndLocal,
            new DateTime(2026, 7, 10, 12, 0, 10, DateTimeKind.Local),
            new DateTime(2026, 7, 10, 12, 0, 50, DateTimeKind.Local));

        Assert.Null(plan);
    }
}
