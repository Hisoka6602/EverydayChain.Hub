using EverydayChain.Hub.Infrastructure.Repositories;

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
            out var skippedExistingCount);

        Assert.Equal(1, skippedExistingCount);
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
            out var skippedExistingCount);

        Assert.Equal(2, skippedExistingCount);
        Assert.Equal(["BC002", "BC004"], result);
    }

    /// <summary>
    /// 分批逻辑应避免超过指定批次大小。
    /// </summary>
    [Fact]
    public void SplitBarcodeBatches_ShouldSplitByConfiguredBatchSize()
    {
        var candidateBarcodes = Enumerable.Range(1, 4501)
            .Select(index => $"BC{index:D4}")
            .ToArray();

        var batches = BusinessTaskSeedRepository.SplitBarcodeBatches(candidateBarcodes, 2000);

        Assert.Equal(3, batches.Count);
        Assert.Equal(2000, batches[0].Count);
        Assert.Equal(2000, batches[1].Count);
        Assert.Equal(501, batches[2].Count);
    }
}
