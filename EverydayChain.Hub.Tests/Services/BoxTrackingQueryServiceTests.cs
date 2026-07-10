using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 验证箱号追踪查询服务。
/// </summary>
public sealed class BoxTrackingQueryServiceTests
{
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
        Assert.Equal("已扫描", item.Status);
    }

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

    [Fact]
    public async Task QueryAsync_ShouldReusePageCacheWithinSameTimeBucket()
    {
        var businessTaskRepository = new InMemoryBusinessTaskRepository();
        var scanLogRepository = new InMemoryScanLogRepository();
        var service = new BoxTrackingQueryService(
            scanLogRepository,
            businessTaskRepository,
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions
            {
                Enabled = true,
                AggregateTimeBucketSeconds = 30,
                BoxTrackingSeconds = 10
            });
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddHours(1);

        await scanLogRepository.SaveAsync(new ScanLogEntity
        {
            Barcode = "BOX-CACHE-001",
            DeviceCode = "SCN-CACHE-01",
            IsMatched = false,
            ScanTimeLocal = start.AddMinutes(10),
            CreatedTimeLocal = start.AddMinutes(10)
        }, CancellationToken.None);

        _ = await service.QueryAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start.AddSeconds(5),
            EndTimeLocal = end.AddSeconds(5),
            BoxId = "BOX-CACHE-001",
            PageNumber = 1,
            PageSize = 20
        }, CancellationToken.None);

        _ = await service.QueryAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start.AddSeconds(20),
            EndTimeLocal = end.AddSeconds(20),
            BoxId = "BOX-CACHE-001",
            PageNumber = 1,
            PageSize = 20
        }, CancellationToken.None);

        Assert.Equal(1, scanLogRepository.QueryPageCallCount);
    }

    [Fact]
    public async Task QueryAllAsync_ShouldReuseRangeCacheWithinSameTimeBucket()
    {
        var businessTaskRepository = new InMemoryBusinessTaskRepository();
        var scanLogRepository = new InMemoryScanLogRepository();
        var service = new BoxTrackingQueryService(
            scanLogRepository,
            businessTaskRepository,
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions
            {
                Enabled = true,
                AggregateTimeBucketSeconds = 30,
                BoxTrackingSeconds = 10
            });
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddHours(1);

        await scanLogRepository.SaveAsync(new ScanLogEntity
        {
            Barcode = "BOX-CACHE-ALL-001",
            DeviceCode = "SCN-CACHE-ALL-01",
            IsMatched = false,
            ScanTimeLocal = start.AddMinutes(10),
            CreatedTimeLocal = start.AddMinutes(10)
        }, CancellationToken.None);

        _ = await service.QueryAllAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start.AddSeconds(5),
            EndTimeLocal = end.AddSeconds(5),
            BoxId = "BOX-CACHE-ALL-001"
        }, CancellationToken.None);

        _ = await service.QueryAllAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start.AddSeconds(20),
            EndTimeLocal = end.AddSeconds(20),
            BoxId = "BOX-CACHE-ALL-001"
        }, CancellationToken.None);

        Assert.Equal(1, scanLogRepository.QueryRangeCallCount);
    }

    /// <summary>
    /// 记录分页路径是否被命中的扫描日志仓储探针。
    /// </summary>
    private sealed class ProbeScanLogRepository : IScanLogRepository
    {
        /// <summary>
        /// 存储 _logs 字段。
        /// </summary>
        private readonly List<ScanLogEntity> _logs = [];

        /// <summary>
        /// 存储 _nextId 字段。
        /// </summary>
        private long _nextId = 1;

        /// <summary>
        /// 获取或设置 QueryPageCallCount。
        /// </summary>
        public int QueryPageCallCount { get; private set; }

        /// <summary>
        /// 获取或设置 QueryRangeCallCount。
        /// </summary>
        public int QueryRangeCallCount { get; private set; }

        /// <summary>
        /// 保存扫描日志。
        /// </summary>
        public Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
        {
            entity.Id = _nextId++;
            _logs.Add(entity);
            return Task.CompletedTask;
        }

        public Task<ScanLogRecognitionAggregate> AggregateRecognitionAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
        {
            return Task.FromResult(new ScanLogRecognitionAggregate());
        }

        /// <summary>
        /// 查询扫描日志范围结果。
        /// </summary>
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
        /// 查询扫描日志分页结果。
        /// </summary>
        public Task<(int TotalCount, IReadOnlyList<ScanLogEntity> Items)> QueryPageAsync(
            DateTime startTimeLocal,
            DateTime endTimeLocal,
            string? barcode,
            string? deviceCode,
            int skip,
            int take,
            CancellationToken ct)
        {
            // 步骤：累计调用次数并返回当前探针仓储中的分页结果。
            QueryPageCallCount++;
            IReadOnlyList<ScanLogEntity> items = _logs.Skip(skip).Take(take).ToList();
            return Task.FromResult((_logs.Count, items));
        }
    }
}
