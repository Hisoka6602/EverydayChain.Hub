using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Infrastructure.Repositories;
using System.Reflection;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 业务任务模拟补数仓储测试。
/// </summary>
public sealed class BusinessTaskSeedRepositoryTests
{
    /// <summary>
    /// 非法表名应校验失败。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("business_tasks")]
    [InlineData("business_tasks_2026")]
    [InlineData("business_tasks_202613")]
    [InlineData("business_tasks_2026ab")]
    [InlineData("business_tasks_202604;drop table")]
    public void TryParseTargetTableName_ShouldFail_WhenTargetTableNameInvalid(string targetTableName)
    {
        var success = BusinessTaskSeedRepository.TryParseTargetTableName(targetTableName, out var normalizedTableName, out var suffix);

        Assert.False(success);
        Assert.Equal(string.Empty, normalizedTableName);
        Assert.Equal(string.Empty, suffix);
    }

    /// <summary>
    /// 合法表名应解析出后缀。
    /// </summary>
    [Fact]
    public void TryParseTargetTableName_ShouldParseSuffix_WhenTargetTableNameValid()
    {
        var success = BusinessTaskSeedRepository.TryParseTargetTableName(" business_tasks_202604 ", out var normalizedTableName, out var suffix);

        Assert.True(success);
        Assert.Equal("business_tasks_202604", normalizedTableName);
        Assert.Equal("_202604", suffix);
    }

    /// <summary>
    /// 已存在 manual_seed + BusinessKey 条码应跳过。
    /// </summary>
    [Fact]
    public void FilterInsertBarcodes_ShouldSkipExistingManualSeedBusinessKeys()
    {
        var result = BusinessTaskSeedRepository.FilterInsertBarcodes(
            ["BC001", "BC002", "BC003"],
            new HashSet<string>(StringComparer.Ordinal)
            {
                "BC002"
            },
            out var skippedBarcodes);

        Assert.Equal(["BC002"], skippedBarcodes);
        Assert.Equal(["BC001", "BC003"], result);
    }

    /// <summary>
    /// 过滤已存在条码时不应改写既有集合。
    /// </summary>
    [Fact]
    public void FilterInsertBarcodes_ShouldNotUpdateExistingSet()
    {
        var existingSet = new HashSet<string>(StringComparer.Ordinal)
        {
            "BC001"
        };

        _ = BusinessTaskSeedRepository.FilterInsertBarcodes(
            ["BC001", "BC002"],
            existingSet,
            out _);

        Assert.Single(existingSet);
        Assert.Contains("BC001", existingSet);
    }

    /// <summary>
    /// 批量插入候选应仅保留不存在数据。
    /// </summary>
    [Fact]
    public void FilterInsertBarcodes_ShouldKeepOnlyNotExistingCandidates()
    {
        var result = BusinessTaskSeedRepository.FilterInsertBarcodes(
            ["BC001", "BC002", "BC003", "BC004"],
            new HashSet<string>(StringComparer.Ordinal)
            {
                "BC001",
                "BC003"
            },
            out var skippedBarcodes);

        Assert.Equal(["BC001", "BC003"], skippedBarcodes);
        Assert.Equal(["BC002", "BC004"], result);
    }

    /// <summary>
    /// 成功结果应包含插入与跳过条码明细。
    /// </summary>
    [Fact]
    public void BuildSuccessResult_ShouldContainInsertedAndSkippedBarcodes()
    {
        var result = InvokeBuildSuccessResult(
            "business_tasks_202604",
            2,
            1,
            ["BC002"],
            ["BC001", "BC003"],
            "模拟补数写入成功。");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.InsertedCount);
        Assert.Equal(1, result.SkippedExistingCount);
        Assert.Equal(["BC001", "BC003"], result.InsertedBarcodes);
        Assert.Equal(["BC002"], result.SkippedBarcodes);
    }

    /// <summary>
    /// 无新增场景应返回空插入集合与非空跳过集合。
    /// </summary>
    [Fact]
    public void BuildSuccessResult_ShouldReturnEmptyInsertedBarcodes_WhenNoBarcodeInserted()
    {
        var result = InvokeBuildSuccessResult(
            "business_tasks_202604",
            0,
            2,
            ["BC001", "BC002"],
            [],
            "模拟补数执行完成，未新增数据。");

        Assert.Empty(result.InsertedBarcodes);
        Assert.Equal(["BC001", "BC002"], result.SkippedBarcodes);
        Assert.NotNull(result.InsertedBarcodes);
        Assert.NotNull(result.SkippedBarcodes);
    }

    /// <summary>
    /// 反射调用仓储私有成功结果构建方法。
    /// </summary>
    /// <param name="targetTableName">目标表名。</param>
    /// <param name="insertedCount">插入数量。</param>
    /// <param name="skippedExistingCount">跳过数量。</param>
    /// <param name="skippedBarcodes">跳过条码。</param>
    /// <param name="insertedBarcodes">插入条码。</param>
    /// <param name="message">结果消息。</param>
    /// <returns>成功结果。</returns>
    private static BusinessTaskSeedResult InvokeBuildSuccessResult(
        string targetTableName,
        int insertedCount,
        int skippedExistingCount,
        IReadOnlyList<string> skippedBarcodes,
        IReadOnlyList<string> insertedBarcodes,
        string message)
    {
        var method = typeof(BusinessTaskSeedRepository).GetMethod(
            "BuildSuccessResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var result = method!.Invoke(
            null,
            [targetTableName, insertedCount, skippedExistingCount, skippedBarcodes, insertedBarcodes, message]);
        var typedResult = Assert.IsType<BusinessTaskSeedResult>(result);
        return typedResult;
    }
}
