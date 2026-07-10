using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.Tests.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义 SyncTaskConfigRepositoryTests 类型。
/// </summary>
public class SyncTaskConfigRepositoryTests
{
    /// <summary>
    /// 验证空白同步模式会回落为主键合并模式。
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
    /// 验证状态驱动模式下显式 null 的待处理状态值会被保留为 null。
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
        Assert.False(definition.StatusConsumeProfile.IgnorePendingStatusValue);
        Assert.Equal(5000, definition.StatusConsumeProfile.BatchSize);
        Assert.Equal("TASKPROCESS", definition.StatusConsumeProfile.StatusColumnName);
    }

    /// <summary>
    /// 验证待处理状态值包含前后空白时会被正确裁剪。
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
    /// 验证仅包含空白的待处理状态值会按 null 语义兼容处理。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenStatusDrivenAndPendingBecomesBlank_ShouldTreatAsNull()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = "   ";
            table.ShouldWriteBackRemoteStatus = true;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.Null(definition.StatusConsumeProfile!.PendingStatusValue);
    }

    /// <summary>
    /// 验证启用忽略待处理状态值时会正确映射开关。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenStatusDrivenAndIgnorePendingStatusEnabled_ShouldMapFlag()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = "DONE";
            table.IgnorePendingStatusValue = true;
            table.ShouldWriteBackRemoteStatus = false;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.NotNull(definition.StatusConsumeProfile);
        Assert.True(definition.StatusConsumeProfile!.IgnorePendingStatusValue);
        Assert.Equal("DONE", definition.StatusConsumeProfile.PendingStatusValue);
    }

    /// <summary>
    /// 验证启用忽略待处理状态值时，空白 PendingStatusValue 不再输出误导性告警。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenIgnorePendingStatusEnabledAndPendingBlank_ShouldNotWarn()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.PendingStatusValue = " ";
            table.IgnorePendingStatusValue = true;
            table.ShouldWriteBackRemoteStatus = false;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var definition = await repository.GetByTableCodeAsync("T1", CancellationToken.None);

        Assert.NotNull(definition.StatusConsumeProfile);
        Assert.True(definition.StatusConsumeProfile!.IgnorePendingStatusValue);
        Assert.DoesNotContain(logger.Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("PendingStatusValue", StringComparison.Ordinal));
    }

    /// <summary>
    /// 验证忽略待处理状态值时禁止同时启用远端回写，避免误更新远端状态。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenIgnorePendingStatusEnabledAndWriteBackEnabled_ShouldThrow()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.IgnorePendingStatusValue = true;
            table.ShouldWriteBackRemoteStatus = true;
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetByTableCodeAsync("T1", CancellationToken.None));

        Assert.Contains("IgnorePendingStatusValue", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ShouldWriteBackRemoteStatus", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 验证忽略待处理状态值时必须保留游标列，避免无边界全表扫描。
    /// </summary>
    [Fact]
    public async Task GetByTableCodeAsync_WhenIgnorePendingStatusEnabledAndCursorColumnBlank_ShouldThrow()
    {
        var options = BuildOptions(table =>
        {
            table.SyncMode = nameof(SyncMode.StatusDriven);
            table.IgnorePendingStatusValue = true;
            table.ShouldWriteBackRemoteStatus = false;
            table.CursorColumn = " ";
        });
        var logger = new TestLogger<SyncTaskConfigRepository>();
        var repository = new SyncTaskConfigRepository(Options.Create(options), logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetByTableCodeAsync("T1", CancellationToken.None));

        Assert.Contains("CursorColumn", exception.Message, StringComparison.Ordinal);
        Assert.Contains("IgnorePendingStatusValue", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 验证禁用远端回写时仍可正常构建状态消费配置。
    /// </summary>
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

    /// <summary>
    /// 验证状态驱动模式下的回写审计列会被正确映射。
    /// </summary>
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

    /// <summary>
    /// 验证基础投影列会被正确映射。
    /// </summary>
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

    /// <summary>
    /// 验证扩展投影列会被正确映射。
    /// </summary>
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

    /// <summary>
    /// 构建测试用同步配置。
    /// </summary>
    /// <param name="configureTable">用于覆盖单表配置的委托。</param>
    /// <returns>测试用同步作业配置。</returns>
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

