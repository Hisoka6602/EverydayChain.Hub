using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 验证箱子追踪查询服务。
/// </summary>
public sealed class BoxTrackingQueryServiceTests
{
    /// <summary>
    /// 验证箱子追踪查询能够返回完整扫描轨迹行。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldReturnScanTraceRows()
    {
        var businessTaskRepository = new InMemoryBusinessTaskRepository();
        var scanLogRepository = new InMemoryScanLogRepository();
        var service = new BoxTrackingQueryService(scanLogRepository, businessTaskRepository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        var task = new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-001",
            Barcode = "BOX-001",
            WaveCode = "WAVE-001",
            OrderId = "ORDER-001",
            StoreId = "STORE-001",
            StoreName = "Store One",
            ProductCode = "SKU-001",
            PickLocation = "LOC-001",
            TargetChuteCode = "B-07",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        };
        await businessTaskRepository.SaveAsync(task, CancellationToken.None);
        await scanLogRepository.SaveAsync(new ScanLogEntity
        {
            BusinessTaskId = task.Id,
            TaskCode = task.TaskCode,
            Barcode = "BOX-001",
            DeviceCode = "SCN-01",
            IsMatched = true,
            ScanTimeLocal = start.AddHours(1),
            CreatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var result = await service.QueryAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("BOX-001", item.BoxId);
        Assert.Equal("TASK-001", item.TaskCode);
        Assert.Equal("WAVE-001", item.WaveCode);
        Assert.Equal("ORDER-001", item.OrderId);
        Assert.Equal("STORE-001", item.StoreId);
        Assert.Equal("Store One", item.StoreName);
        Assert.Equal("SKU-001", item.ProductCode);
        Assert.Equal("LOC-001", item.PickLocation);
        Assert.Equal("SCN-01", item.Scanner);
        Assert.Equal("B-07", item.Chute);
        Assert.Equal("Scanned", item.Status);
    }

    /// <summary>
    /// 验证箱子追踪查询能够按 chute 过滤。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldFilterByChuteCode()
    {
        var businessTaskRepository = new InMemoryBusinessTaskRepository();
        var scanLogRepository = new InMemoryScanLogRepository();
        var service = new BoxTrackingQueryService(scanLogRepository, businessTaskRepository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        var task = new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-002",
            Barcode = "BOX-002",
            TargetChuteCode = "C-03",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        };
        await businessTaskRepository.SaveAsync(task, CancellationToken.None);
        await scanLogRepository.SaveAsync(new ScanLogEntity
        {
            BusinessTaskId = task.Id,
            TaskCode = task.TaskCode,
            Barcode = "BOX-002",
            DeviceCode = "SCN-02",
            IsMatched = true,
            ScanTimeLocal = start.AddHours(2),
            CreatedTimeLocal = start.AddHours(2)
        }, CancellationToken.None);

        var result = await service.QueryAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            ChuteCode = "B-07"
        }, CancellationToken.None);

        Assert.Empty(result.Items);
    }

    /// <summary>
    /// 验证无任务级筛选时会命中数据库分页路径。
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithoutTaskLevelFilters_ShouldUseDatabasePagingPath()
    {
        var businessTaskRepository = new InMemoryBusinessTaskRepository();
        var scanLogRepository = new ProbeScanLogRepository();
        var service = new BoxTrackingQueryService(scanLogRepository, businessTaskRepository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await scanLogRepository.SaveAsync(new ScanLogEntity
        {
            Barcode = "BOX-100",
            DeviceCode = "SCN-100",
            IsMatched = false,
            ScanTimeLocal = start.AddMinutes(10),
            CreatedTimeLocal = start.AddMinutes(10)
        }, CancellationToken.None);

        _ = await service.QueryAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageNumber = 1,
            PageSize = 20
        }, CancellationToken.None);

        Assert.Equal(1, scanLogRepository.QueryPageCallCount);
        Assert.Equal(0, scanLogRepository.QueryRangeCallCount);
    }

    /// <summary>
    /// 记录分页路径是否被命中的扫描日志仓储探针。
    /// </summary>
    private sealed class ProbeScanLogRepository : IScanLogRepository
    {
        /// <summary>
        /// 存储扫描日志集合。
        /// </summary>
        private readonly List<ScanLogEntity> _logs = [];

        /// <summary>
        /// 存储自增主键值。
        /// </summary>
        private long _nextId = 1;

        /// <summary>
        /// 获取分页查询调用次数。
        /// </summary>
        public int QueryPageCallCount { get; private set; }

        /// <summary>
        /// 获取范围查询调用次数。
        /// </summary>
        public int QueryRangeCallCount { get; private set; }

        /// <summary>
        /// 保存扫描日志。
        /// </summary>
        /// <param name="entity">扫描日志实体。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>异步任务。</returns>
        public Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
        {
            entity.Id = _nextId++;
            _logs.Add(entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 聚合识别率统计。
        /// </summary>
        /// <param name="startTimeLocal">开始时间。</param>
        /// <param name="endTimeLocal">结束时间。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>识别率统计结果。</returns>
        public Task<ScanLogRecognitionAggregate> AggregateRecognitionAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
        {
            return Task.FromResult(new ScanLogRecognitionAggregate());
        }

        /// <summary>
        /// 执行范围查询。
        /// </summary>
        /// <param name="startTimeLocal">开始时间。</param>
        /// <param name="endTimeLocal">结束时间。</param>
        /// <param name="barcode">条码。</param>
        /// <param name="deviceCode">设备号。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>范围查询结果。</returns>
        public Task<IReadOnlyList<ScanLogEntity>> QueryRangeAsync(
            DateTime startTimeLocal,
            DateTime endTimeLocal,
            string? barcode,
            string? deviceCode,
            CancellationToken ct)
        {
            QueryRangeCallCount++;
            return Task.FromResult<IReadOnlyList<ScanLogEntity>>(_logs);
        }

        /// <summary>
        /// 执行分页查询。
        /// </summary>
        /// <param name="startTimeLocal">开始时间。</param>
        /// <param name="endTimeLocal">结束时间。</param>
        /// <param name="barcode">条码。</param>
        /// <param name="deviceCode">设备号。</param>
        /// <param name="skip">跳过条数。</param>
        /// <param name="take">获取条数。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>分页查询结果。</returns>
        public Task<(int TotalCount, IReadOnlyList<ScanLogEntity> Items)> QueryPageAsync(
            DateTime startTimeLocal,
            DateTime endTimeLocal,
            string? barcode,
            string? deviceCode,
            int skip,
            int take,
            CancellationToken ct)
        {
            QueryPageCallCount++;
            IReadOnlyList<ScanLogEntity> items = _logs.Skip(skip).Take(take).ToList();
            return Task.FromResult((_logs.Count, items));
        }
    }
}
