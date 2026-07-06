using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Infrastructure.Repositories;
using System.Reflection;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskSeedRepositoryTests
{
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

    [Fact]
    public void TryParseTargetTableName_ShouldParseSuffix_WhenTargetTableNameValid()
    {
        var success = BusinessTaskSeedRepository.TryParseTargetTableName(" business_tasks_202604 ", out var normalizedTableName, out var suffix);

        Assert.True(success);
        Assert.Equal("business_tasks_202604", normalizedTableName);
        Assert.Equal("_202604", suffix);
    }

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
    /// 执行当前方法。
    /// </summary>
    private static BusinessTaskSeedResult InvokeBuildSuccessResult(
        string targetTableName,
        int insertedCount,
        int skippedExistingCount,
        IReadOnlyList<string> skippedBarcodes,
        IReadOnlyList<string> insertedBarcodes,
        string message)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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

