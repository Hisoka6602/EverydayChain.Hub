using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.Infrastructure.Sync.Services;
using EverydayChain.Hub.Tests.Services;
using EverydayChain.Hub.Tests.Sync.Fakes;

namespace EverydayChain.Hub.Tests.Sync;

/// <summary>
/// 定义 BusinessTaskStatusConsumeServiceTests 类型。
/// </summary>
public class BusinessTaskStatusConsumeServiceTests
{
    [Fact]
    public async Task ConsumeAsync_WhenWriteBackEnabled_ShouldProjectAndWriteBack()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["CARTONNO"] = "C1",
                ["WAVENO"] = "W1",
                ["DESCR"] = "R1",
                ["WORKINGAREA"] = "1",
                ["DOCNO"] = "ORDER-1",
                ["CONSIGNEEID"] = "STORE-1",
                ["MENDIAN"] = "Store One",
                ["ADDTIME"] = new DateTime(2032, 1, 1, 8, 0, 0, DateTimeKind.Local),
                ["__RowId"] = "ROW-1"
            }
        ]);
        reader.Pages.Enqueue([]);
        var writer = new FakeOracleRemoteStatusWriter();
        var projectionService = new BusinessTaskProjectionService();
        var repository = new InMemoryBusinessTaskRepository();
        var logger = new TestLogger<BusinessTaskStatusConsumeService>();
        var service = new BusinessTaskStatusConsumeService(reader, writer, projectionService, repository, logger);

        var result = await service.ConsumeAsync(BuildSplitDefinition(true), "B1", default, CancellationToken.None);

        Assert.Equal(1, result.ReadCount);
        Assert.Equal(1, result.AppendCount);
        Assert.Equal(1, result.WriteBackCount);
        Assert.Equal(new DateTime(2032, 1, 1, 8, 0, 0, DateTimeKind.Local), result.LastSuccessCursorLocal);
        var entity = await repository.FindByTaskCodeAsync("C1", CancellationToken.None);
        Assert.NotNull(entity);
        Assert.Equal("W1", entity!.WaveCode);
        Assert.Equal("1", entity.WorkingArea);
        Assert.Equal(BusinessTaskSourceType.Split, entity.SourceType);
        Assert.Equal("ORDER-1", entity.OrderId);
        Assert.Equal("STORE-1", entity.StoreId);
        Assert.Equal("Store One", entity.StoreName);
        Assert.Null(entity.ProductCode);
        Assert.Null(entity.PickLocation);
    }

    [Fact]
    public async Task ConsumeAsync_WhenWriteBackDisabled_ShouldSkipRemoteWriteBack()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SKUID"] = "SKU1",
                ["WAVENO"] = "W2",
                ["DESCR"] = "R2",
                ["DOCNO"] = "ORDER-2",
                ["CONSIGNEEID"] = "STORE-2",
                ["MENDIAN"] = "Store Two",
                ["SKU"] = "SKU-CODE-2",
                ["LOCATION"] = "LOC-2",
                ["ADDTIME"] = new DateTime(2032, 1, 2, 8, 0, 0, DateTimeKind.Local),
                ["__RowId"] = "ROW-2"
            }
        ]);
        reader.Pages.Enqueue([]);
        var writer = new FakeOracleRemoteStatusWriter();
        var projectionService = new BusinessTaskProjectionService();
        var repository = new InMemoryBusinessTaskRepository();
        var logger = new TestLogger<BusinessTaskStatusConsumeService>();
        var service = new BusinessTaskStatusConsumeService(reader, writer, projectionService, repository, logger);

        var result = await service.ConsumeAsync(BuildFullCaseDefinition(false), "B2", default, CancellationToken.None);

        Assert.Equal(1, result.ReadCount);
        Assert.Equal(1, result.AppendCount);
        Assert.Equal(0, result.WriteBackCount);
        Assert.Equal(new DateTime(2032, 1, 2, 8, 0, 0, DateTimeKind.Local), result.LastSuccessCursorLocal);
        Assert.Equal(0, writer.TotalWriteBackRows);
        var entity = await repository.FindByTaskCodeAsync("SKU1", CancellationToken.None);
        Assert.NotNull(entity);
        Assert.Equal("ORDER-2", entity!.OrderId);
        Assert.Equal("STORE-2", entity.StoreId);
        Assert.Equal("Store Two", entity.StoreName);
        Assert.Equal("SKU-CODE-2", entity.ProductCode);
        Assert.Equal("LOC-2", entity.PickLocation);
    }

    [Fact]
    public async Task ConsumeAsync_WhenRowIdMissing_ShouldSkipWriteBack()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["CARTONNO"] = "C2",
                ["WAVENO"] = "W3",
                ["DESCR"] = "R3",
                ["ADDTIME"] = new DateTime(2032, 1, 3, 8, 0, 0, DateTimeKind.Local),
                ["__RowId"] = null
            }
        ]);
        reader.Pages.Enqueue([]);
        var writer = new FakeOracleRemoteStatusWriter();
        var projectionService = new BusinessTaskProjectionService();
        var repository = new InMemoryBusinessTaskRepository();
        var logger = new TestLogger<BusinessTaskStatusConsumeService>();
        var service = new BusinessTaskStatusConsumeService(reader, writer, projectionService, repository, logger);

        var result = await service.ConsumeAsync(BuildSplitDefinition(true), "B3", default, CancellationToken.None);

        Assert.Equal(1, result.AppendCount);
        Assert.Equal(0, result.WriteBackCount);
        Assert.Equal(1, result.SkippedWriteBackCount);
        Assert.Equal(0, writer.TotalWriteBackRows);
    }

    [Fact]
    public async Task ConsumeAsync_WhenFixedFirstPageAndNoProjectionRows_ShouldBreak()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["CARTONNO"] = null,
                ["__RowId"] = "ROW-3"
            }
        ]);
        var writer = new FakeOracleRemoteStatusWriter();
        var projectionService = new BusinessTaskProjectionService();
        var repository = new InMemoryBusinessTaskRepository();
        var logger = new TestLogger<BusinessTaskStatusConsumeService>();
        var service = new BusinessTaskStatusConsumeService(reader, writer, projectionService, repository, logger);

        var result = await service.ConsumeAsync(BuildSplitDefinition(true), "B4", default, CancellationToken.None);

        Assert.Equal(1, result.ReadCount);
        Assert.Equal(0, result.AppendCount);
        Assert.Equal(0, result.WriteBackCount);
        Assert.Equal(0, writer.TotalWriteBackRows);
        Assert.Contains(logger.Logs, log => log.Message.Contains("无可投影行", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConsumeAsync_WhenCursorColumnContainsBusinessTime_ShouldUseBusinessTime()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        var expectedProjectedTime = new DateTime(2032, 4, 20, 10, 30, 0, DateTimeKind.Local);
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["CARTONNO"] = "C-BIZ-1",
                ["WAVENO"] = "W-BIZ-1",
                ["DESCR"] = "R-BIZ-1",
                ["ADDTIME"] = expectedProjectedTime,
                ["__RowId"] = "ROW-BIZ-1"
            }
        ]);
        reader.Pages.Enqueue([]);
        var writer = new FakeOracleRemoteStatusWriter();
        var projectionService = new BusinessTaskProjectionService();
        var repository = new InMemoryBusinessTaskRepository();
        var logger = new TestLogger<BusinessTaskStatusConsumeService>();
        var service = new BusinessTaskStatusConsumeService(reader, writer, projectionService, repository, logger);
        var window = new SyncWindow(
            new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Local),
            /// <summary>
            /// 执行 DateTime 方法。
            /// </summary>
            new DateTime(2026, 4, 20, 23, 59, 59, DateTimeKind.Local));
        var beforeConsumeLocal = DateTime.Now;

        var result = await service.ConsumeAsync(BuildSplitDefinition(false), "B5", window, CancellationToken.None);
        var afterConsumeLocal = DateTime.Now;

        Assert.Equal(1, result.AppendCount);
        Assert.Equal(expectedProjectedTime, result.LastSuccessCursorLocal);
        var entity = await repository.FindByTaskCodeAsync("C-BIZ-1", CancellationToken.None);
        Assert.NotNull(entity);
        Assert.Equal(expectedProjectedTime, entity!.CreatedTimeLocal);
        Assert.InRange(entity.UpdatedTimeLocal, beforeConsumeLocal, afterConsumeLocal);
        Assert.NotEqual(DateTime.Now, entity.CreatedTimeLocal);
    }

    [Fact]
    public async Task ConsumeAsync_WhenCursorTimeInvalid_ShouldThrow()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["CARTONNO"] = "C-BIZ-2",
                ["WAVENO"] = "W-BIZ-2",
                ["DESCR"] = "R-BIZ-2",
                ["ADDTIME"] = "INVALID-TIME",
                ["__RowId"] = "ROW-BIZ-2"
            }
        ]);
        reader.Pages.Enqueue([]);
        var writer = new FakeOracleRemoteStatusWriter();
        var projectionService = new BusinessTaskProjectionService();
        var repository = new InMemoryBusinessTaskRepository();
        var logger = new TestLogger<BusinessTaskStatusConsumeService>();
        var service = new BusinessTaskStatusConsumeService(reader, writer, projectionService, repository, logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConsumeAsync(BuildSplitDefinition(true), "B6", default, CancellationToken.None));

        Assert.Contains("远端业务时间缺失", exception.Message, StringComparison.Ordinal);
        Assert.Contains("无法确定分表月份", exception.Message, StringComparison.Ordinal);
        Assert.Null(await repository.FindByTaskCodeAsync("C-BIZ-2", CancellationToken.None));
        Assert.Equal(0, writer.TotalWriteBackRows);
        Assert.Contains(logger.Logs, log =>
            log.Level == Microsoft.Extensions.Logging.LogLevel.Error
            && log.Message.Contains("远端业务时间缺失", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConsumeAsync_WhenCursorTimeMissing_ShouldThrow()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["CARTONNO"] = "C-BIZ-3",
                ["WAVENO"] = "W-BIZ-3",
                ["DESCR"] = "R-BIZ-3",
                ["__RowId"] = "ROW-BIZ-3"
            }
        ]);
        reader.Pages.Enqueue([]);
        var writer = new FakeOracleRemoteStatusWriter();
        var projectionService = new BusinessTaskProjectionService();
        var repository = new InMemoryBusinessTaskRepository();
        var logger = new TestLogger<BusinessTaskStatusConsumeService>();
        var service = new BusinessTaskStatusConsumeService(reader, writer, projectionService, repository, logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConsumeAsync(BuildSplitDefinition(true), "B7", default, CancellationToken.None));

        Assert.Contains("远端业务时间缺失", exception.Message, StringComparison.Ordinal);
        Assert.Contains("无法确定分表月份", exception.Message, StringComparison.Ordinal);
        Assert.Null(await repository.FindByTaskCodeAsync("C-BIZ-3", CancellationToken.None));
        Assert.Equal(0, writer.TotalWriteBackRows);
        Assert.Contains(logger.Logs, log =>
            log.Level == Microsoft.Extensions.Logging.LogLevel.Error
            && log.Message.Contains("远端业务时间缺失", StringComparison.Ordinal));
    }

    private static SyncTableDefinition BuildSplitDefinition(bool writeBack)
    {
        return new SyncTableDefinition
        {
            TableCode = "WmsSplitPickToLightCarton",
            Enabled = true,
            SyncMode = SyncMode.StatusDriven,
            SourceSchema = "WMS_USER_431",
            SourceTable = "IDX_PICKTOLIGHT_CARTON1",
            TargetLogicalTable = "business_tasks",
            CursorColumn = "ADDTIME",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKeyColumn = "CARTONNO",
            BarcodeColumn = "CARTONNO",
            WaveCodeColumn = "WAVENO",
            WaveRemarkColumn = "DESCR",
            WorkingAreaColumn = "WORKINGAREA",
            OrderIdColumn = "DOCNO",
            StoreIdColumn = "CONSIGNEEID",
            StoreNameColumn = "MENDIAN",
            StatusConsumeProfile = new RemoteStatusConsumeProfile
            {
                StatusColumnName = "TASKPROCESS",
                PendingStatusValue = "N",
                CompletedStatusValue = "Y",
                ShouldWriteBackRemoteStatus = writeBack,
                BatchSize = 500
            }
        };
    }

    private static SyncTableDefinition BuildFullCaseDefinition(bool writeBack)
    {
        return new SyncTableDefinition
        {
            TableCode = "WmsPickToWcs",
            Enabled = true,
            SyncMode = SyncMode.StatusDriven,
            SourceSchema = "WMS_USER_431",
            SourceTable = "IDX_PICKTOWCS2",
            TargetLogicalTable = "business_tasks",
            CursorColumn = "ADDTIME",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKeyColumn = "SKUID",
            BarcodeColumn = "SKUID",
            WaveCodeColumn = "WAVENO",
            WaveRemarkColumn = "DESCR",
            WorkingAreaColumn = "WORKINGAREA",
            OrderIdColumn = "DOCNO",
            StoreIdColumn = "CONSIGNEEID",
            StoreNameColumn = "MENDIAN",
            ProductCodeColumn = "SKU",
            PickLocationColumn = "LOCATION",
            StatusConsumeProfile = new RemoteStatusConsumeProfile
            {
                StatusColumnName = "TASKPROCESS",
                PendingStatusValue = "N",
                CompletedStatusValue = "Y",
                ShouldWriteBackRemoteStatus = writeBack,
                BatchSize = 500
            }
        };
    }
}

