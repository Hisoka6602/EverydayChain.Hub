using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.Tests.Services;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// SyncTaskConfigRepository 配置映射测试。
/// </summary>
public class SyncTaskConfigRepositoryTests
{
    /// <summary>
    /// 未配置 SyncMode 时应默认映射为 KeyedMerge。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenSyncModeIsBlank_ShouldDefaultToKeyedMerge()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = "   ";
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.Equal(SyncMode.KeyedMerge, definition.SyncMode);
        Assert.Null(definition.StatusConsumeProfile);
    }

    /// <summary>
    /// StatusDriven 模式下 PendingStatusValue 为 null 时应保持 null 语义。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenStatusDrivenAndPendingNull_ShouldKeepNullPendingValue()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.StatusColumnName = "TASKPROCESS";
            table.PendingStatusValue = null;
            table.CompletedStatusValue = "Y";
            table.ShouldWriteBackRemoteStatus = true;
            table.StatusBatchSize = 0;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.Equal(SyncMode.StatusDriven, definition.SyncMode);
        Assert.NotNull(definition.StatusConsumeProfile);
        Assert.Null(definition.StatusConsumeProfile!.PendingStatusValue);
        Assert.Equal(5000, definition.StatusConsumeProfile.BatchSize);
        Assert.Equal("TASKPROCESS", definition.StatusConsumeProfile.StatusColumnName);
    }

    /// <summary>
    /// StatusDriven 模式下 PendingStatusValue 应执行 Trim。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenStatusDrivenAndPendingHasWhitespace_ShouldTrim()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = " N ";
            table.ShouldWriteBackRemoteStatus = true;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.Equal("N", definition.StatusConsumeProfile!.PendingStatusValue);
    }

    /// <summary>
    /// StatusDriven 模式下 PendingStatusValue 去空白后为空时应报错。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenStatusDrivenAndPendingBecomesBlank_ShouldThrow()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = "   ";
            table.ShouldWriteBackRemoteStatus = true;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetByTableCodeAsync("T1", CancellationToken.None));

        Assert.Contains("PendingStatusValue", exception.Message);
    }

    /// <summary>
    /// StatusDriven 模式下禁止关闭远端回写。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenStatusDrivenAndWriteBackDisabled_ShouldThrow()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = "N";
            table.ShouldWriteBackRemoteStatus = false;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetByTableCodeAsync("T1", CancellationToken.None));

        Assert.Contains("ShouldWriteBackRemoteStatus", exception.Message);
    }

    /// <summary>
    /// 构建默认测试配置。
    /// </summary>
    /// <param name="configureTable">表级配置回调。</param>
    /// <returns>同步任务配置。</returns>
    private static SyncJobOptions BuildOptions(Action<SyncTableOptions>? configureTable = null)
    {
        var table = new SyncTableOptions
        {
            TableCode = "T1",
            Enabled = true,
            SourceSchema = "SRC",
            SourceTable = "TAB1",
            TargetLogicalTable = "TAB1",
            CursorColumn = "ADDTIME",
            StartTimeLocal = "2026-01-01 00:00:00",
            Priority = "Low",
            UniqueKeys = ["ID"],
            ExcludedColumns = [],
            Delete = new SyncDeleteOptions
            {
                Enabled = false,
                DryRun = true,
                CompareSegmentSize = 100,
                CompareMaxParallelism = 1,
                Policy = DeletionPolicy.Disabled,
            },
            Retention = new SyncRetentionOptions
            {
                Enabled = false,
                KeepMonths = 3,
                DryRun = true,
                AllowDrop = false,
            },
        };

        configureTable?.Invoke(table);

        return new SyncJobOptions
        {
            PollingIntervalSeconds = 60,
            DefaultMaxLagMinutes = 10,
            MaxParallelTables = 1,
            Tables = [table],
        };
    }
}
