using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.Tests.Services;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncTaskConfigRepositoryTests
{
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

    [Fact]
    public async Task GetByTableCodeAsync_WhenStatusDrivenAndWriteBackDisabled_ShouldSucceedWithFalse()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = "N";
            table.ShouldWriteBackRemoteStatus = false;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.Equal(SyncMode.StatusDriven, definition.SyncMode);
        Assert.NotNull(definition.StatusConsumeProfile);
        Assert.False(definition.StatusConsumeProfile!.ShouldWriteBackRemoteStatus);
    }

    [Fact]
    public async Task GetByTableCodeAsync_WhenStatusDrivenAndAuditColumnsConfigured_ShouldMapAuditColumns()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.ShouldWriteBackRemoteStatus = true;
            table.WriteBackCompletedTimeColumnName = "  FINISH_TIME  ";
            table.WriteBackBatchIdColumnName = "  FINISH_BATCH_ID  ";
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.NotNull(definition.StatusConsumeProfile);
        Assert.Equal("FINISH_TIME", definition.StatusConsumeProfile!.WriteBackCompletedTimeColumnName);
        Assert.Equal("FINISH_BATCH_ID", definition.StatusConsumeProfile.WriteBackBatchIdColumnName);
    }

    [Fact]
    public async Task GetByTableCodeAsync_WhenProjectionColumnsConfigured_ShouldMapProjectionColumns()
    {
        var options = BuildOptions(table =>
        {
            table.SourceType = nameof(BusinessTaskSourceType.Split);
            table.BusinessKeyColumn = "  CARTONNO  ";
            table.BarcodeColumn = "  CARTONNO  ";
            table.WaveCodeColumn = "  WAVENO  ";
            table.WaveRemarkColumn = "  DESCR  ";
            table.WorkingAreaColumn = "  WORKINGAREA  ";
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.Equal(BusinessTaskSourceType.Split, definition.SourceType);
        Assert.Equal("CARTONNO", definition.BusinessKeyColumn);
        Assert.Equal("CARTONNO", definition.BarcodeColumn);
        Assert.Equal("WAVENO", definition.WaveCodeColumn);
        Assert.Equal("DESCR", definition.WaveRemarkColumn);
        Assert.Equal("WORKINGAREA", definition.WorkingAreaColumn);
        Assert.Null(definition.OrderIdColumn);
        Assert.Null(definition.StoreIdColumn);
        Assert.Null(definition.StoreNameColumn);
        Assert.Null(definition.ProductCodeColumn);
        Assert.Null(definition.PickLocationColumn);
    }

    [Fact]
    public async Task GetByTableCodeAsync_WhenExtendedProjectionColumnsConfigured_ShouldMapExtendedProjectionColumns()
    {
        var options = BuildOptions(table =>
        {
            table.SourceType = nameof(BusinessTaskSourceType.FullCase);
            table.BusinessKeyColumn = "  SKUID  ";
            table.OrderIdColumn = "  DOCNO  ";
            table.StoreIdColumn = "  CONSIGNEEID  ";
            table.StoreNameColumn = "  MENDIAN  ";
            table.ProductCodeColumn = "  SKU  ";
            table.PickLocationColumn = "  LOCATION  ";
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.Equal("DOCNO", definition.OrderIdColumn);
        Assert.Equal("CONSIGNEEID", definition.StoreIdColumn);
        Assert.Equal("MENDIAN", definition.StoreNameColumn);
        Assert.Equal("SKU", definition.ProductCodeColumn);
        Assert.Equal("LOCATION", definition.PickLocationColumn);
    }

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

