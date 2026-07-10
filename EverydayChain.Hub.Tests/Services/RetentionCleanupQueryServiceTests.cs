using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义保留期清理审计查询服务测试。
/// </summary>
public sealed class RetentionCleanupQueryServiceTests
{
    [Fact]
    public async Task QueryAsync_ShouldNormalizeFiltersAndMapItems()
    {
        var repository = new InMemoryRetentionCleanupAuditLogRepository();
        repository.Items.Add(new RetentionCleanupAuditLogEntity
        {
            Id = "AUDIT-001",
            BatchId = "BATCH-001",
            TargetCode = "scan_logs-retention",
            LogicalTableName = "scan_logs",
            RetentionMode = "DropShards",
            TimeColumnName = string.Empty,
            KeepMonths = 3,
            IsDryRun = false,
            AllowDelete = true,
            ThresholdTimeLocal = new DateTime(2026, 4, 1, 0, 0, 0),
            ExecutionStage = "Completed",
            ScannedCount = 5,
            CandidateCount = 2,
            DeletedCount = 2,
            Message = "已完成过期分表清理。",
            InstanceId = "host-a-1",
            StartedTimeLocal = new DateTime(2026, 7, 7, 1, 0, 0),
            CompletedTimeLocal = new DateTime(2026, 7, 7, 1, 0, 2)
        });
        repository.Items.Add(new RetentionCleanupAuditLogEntity
        {
            Id = "AUDIT-002",
            BatchId = "BATCH-002",
            TargetCode = "dashboard-task-snapshots-retention",
            LogicalTableName = "dashboard_task_snapshots",
            RetentionMode = "DeleteRows",
            TimeColumnName = "BucketStartLocal",
            KeepMonths = 12,
            IsDryRun = true,
            AllowDelete = false,
            ThresholdTimeLocal = new DateTime(2025, 7, 1, 0, 0, 0),
            ExecutionStage = "Started",
            ScannedCount = 1,
            CandidateCount = 300,
            DeletedCount = 0,
            Message = "仅预演未执行删除。",
            InstanceId = "host-b-2",
            StartedTimeLocal = new DateTime(2026, 7, 6, 1, 0, 0)
        });

        var service = new RetentionCleanupQueryService(repository);

        var result = await service.QueryAsync(
            new RetentionCleanupAuditQueryRequest
            {
                StartTimeLocal = new DateTime(2026, 7, 1, 0, 0, 0),
                EndTimeLocal = new DateTime(2026, 7, 7, 23, 59, 59),
                LogicalTableName = "  scan_logs  ",
                PageNumber = 0,
                PageSize = 0
            },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(50, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal("AUDIT-001", result.Items[0].Id);
        Assert.Equal("scan_logs", result.Items[0].LogicalTableName);
        Assert.Equal("Completed", result.Items[0].ExecutionStage);
    }

    [Fact]
    public async Task QueryAsync_ShouldReuseCacheWithinSameTimeBucket()
    {
        var repository = new InMemoryRetentionCleanupAuditLogRepository();
        repository.Items.Add(new RetentionCleanupAuditLogEntity
        {
            Id = "AUDIT-CACHE-001",
            BatchId = "BATCH-CACHE-001",
            TargetCode = "scan_logs-retention",
            LogicalTableName = "scan_logs",
            RetentionMode = "DropShards",
            TimeColumnName = string.Empty,
            KeepMonths = 3,
            IsDryRun = false,
            AllowDelete = true,
            ThresholdTimeLocal = new DateTime(2026, 4, 1, 0, 0, 0),
            ExecutionStage = "Completed",
            ScannedCount = 5,
            CandidateCount = 2,
            DeletedCount = 2,
            Message = "缓存复用测试",
            InstanceId = "host-cache-1",
            StartedTimeLocal = new DateTime(2026, 7, 7, 1, 5, 0),
            CompletedTimeLocal = new DateTime(2026, 7, 7, 1, 5, 2)
        });

        var service = new RetentionCleanupQueryService(
            repository,
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions
            {
                Enabled = true,
                AggregateTimeBucketSeconds = 30,
                RetentionCleanupSeconds = 10
            });

        _ = await service.QueryAsync(new RetentionCleanupAuditQueryRequest
        {
            StartTimeLocal = new DateTime(2026, 7, 7, 1, 0, 5),
            EndTimeLocal = new DateTime(2026, 7, 7, 2, 0, 5),
            LogicalTableName = "scan_logs",
            PageNumber = 1,
            PageSize = 20
        }, CancellationToken.None);

        _ = await service.QueryAsync(new RetentionCleanupAuditQueryRequest
        {
            StartTimeLocal = new DateTime(2026, 7, 7, 1, 0, 20),
            EndTimeLocal = new DateTime(2026, 7, 7, 2, 0, 20),
            LogicalTableName = "scan_logs",
            PageNumber = 1,
            PageSize = 20
        }, CancellationToken.None);

        Assert.Equal(1, repository.QueryCallCount);
    }
}
