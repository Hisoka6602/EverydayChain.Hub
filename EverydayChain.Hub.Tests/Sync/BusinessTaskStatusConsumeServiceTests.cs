using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.Infrastructure.Sync.Services;
using EverydayChain.Hub.Tests.Services;
using EverydayChain.Hub.Tests.Sync.Fakes;

namespace EverydayChain.Hub.Tests.Sync;

/// <summary>
/// BusinessTaskStatusConsumeService 行为测试。
/// </summary>
public class BusinessTaskStatusConsumeServiceTests
{
    /// <summary>
    /// 启用回写时应写入业务主表并回写远端状态。
    /// </summary>
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
        var entity = await repository.FindByTaskCodeAsync("C1", CancellationToken.None);
        Assert.NotNull(entity);
        Assert.Equal("W1", entity!.WaveCode);
        Assert.Equal(BusinessTaskSourceType.Split, entity.SourceType);
    }

    /// <summary>
    /// 关闭回写时应仅写入本地业务主表，不执行远端状态回写。
    /// </summary>
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
        Assert.Equal(0, writer.TotalWriteBackRows);
    }

    /// <summary>
    /// 缺失 RowId 时不得误回写。
    /// </summary>
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

    /// <summary>
    /// 固定第一页回写模式下，若当前页无可投影行应提前结束，避免死循环。
    /// </summary>
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

    /// <summary>
    /// 源行包含游标业务时间时应按业务时间投影。
    /// </summary>
    [Fact]
    public async Task ConsumeAsync_WhenCursorColumnContainsBusinessTime_ShouldUseBusinessTime()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        var expectedProjectedTime = new DateTime(2026, 4, 20, 10, 30, 0, DateTimeKind.Local);
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
            new DateTime(2026, 4, 20, 23, 59, 59, DateTimeKind.Local));

        var result = await service.ConsumeAsync(BuildSplitDefinition(false), "B5", window, CancellationToken.None);

        Assert.Equal(1, result.AppendCount);
        var entity = await repository.FindByTaskCodeAsync("C-BIZ-1", CancellationToken.None);
        Assert.NotNull(entity);
        Assert.Equal(expectedProjectedTime, entity!.CreatedTimeLocal);
        Assert.Equal(expectedProjectedTime, entity.UpdatedTimeLocal);
    }

    /// <summary>
    /// 构建拆零定义。
    /// </summary>
    /// <param name="writeBack">是否回写。</param>
    /// <returns>同步定义。</returns>
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

    /// <summary>
    /// 构建整件定义。
    /// </summary>
    /// <param name="writeBack">是否回写。</param>
    /// <returns>同步定义。</returns>
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
