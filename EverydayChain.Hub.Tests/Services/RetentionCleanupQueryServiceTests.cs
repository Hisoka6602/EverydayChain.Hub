using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义保留期清理审计查询服务测试。
/// </summary>
public sealed class RetentionCleanupQueryServiceTests
{
    /// <summary>
    /// 查询时应规范化筛选参数并返回映射结果。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldNormalizeFiltersAndMapItems()
    {
        // 步骤：准备两条不同逻辑表的审计记录，验证查询服务只返回命中的一条并回填默认分页值。
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
}
