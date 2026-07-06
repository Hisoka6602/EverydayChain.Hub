using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.Tests.Services;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public class BusinessTaskStatusWriteBackConfigurationTests
{
    [Fact]
    public async Task GetByTableCodeAsync_WhenCompletedStatusValueChanged_ShouldMapFromConfiguration()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = "N";
            table.CompletedStatusValue = "DONE";
            table.ShouldWriteBackRemoteStatus = true;
        });
        var repository = new SyncTaskConfigRepository(Options.Create(options), new TestLogger<SyncTaskConfigRepository>());

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.NotNull(definition.StatusConsumeProfile);
        Assert.Equal("DONE", definition.StatusConsumeProfile!.CompletedStatusValue);
    }

    [Fact]
    public async Task GetByTableCodeAsync_WhenPendingStatusValueIsNull_ShouldKeepNull()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = null;
            table.CompletedStatusValue = "Y";
            table.ShouldWriteBackRemoteStatus = true;
        });
        var repository = new SyncTaskConfigRepository(Options.Create(options), new TestLogger<SyncTaskConfigRepository>());

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.NotNull(definition.StatusConsumeProfile);
        Assert.Null(definition.StatusConsumeProfile!.PendingStatusValue);
    }

    private static SyncJobOptions BuildOptions(Action<SyncTableOptions>? configureTable = null)
    {
        var table = new SyncTableOptions
        {
            TableCode = "T1",
            Enabled = true,
            SourceSchema = "SRC",
            SourceTable = "TAB1",
            TargetLogicalTable = "business_tasks",
            CursorColumn = "ADDTIME",
            StartTimeLocal = "2026-01-01 00:00:00",
            UniqueKeys = ["ID"],
            ExcludedColumns = [],
            Delete = new SyncDeleteOptions
            {
                Enabled = false,
                DryRun = true,
                Policy = DeletionPolicy.Disabled,
                CompareSegmentSize = 100,
                CompareMaxParallelism = 1
            },
            Retention = new SyncRetentionOptions
            {
                Enabled = false,
                KeepMonths = 3,
                DryRun = true,
                AllowDrop = false
            }
        };
        configureTable?.Invoke(table);
        return new SyncJobOptions
        {
            PollingIntervalSeconds = 60,
            DefaultMaxLagMinutes = 10,
            MaxParallelTables = 1,
            Tables = [table]
        };
    }
}

