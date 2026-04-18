using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// SyncExecutionService 状态驱动配置校验测试。
/// </summary>
public class SyncExecutionServiceStatusDrivenValidationTests
{
    /// <summary>未命中路径替身的统一异常消息。</summary>
    private const string UnexpectedCallMessage = "此方法不应在配置校验测试路径中被调用。";

    /// <summary>
    /// 当 TargetLogicalTable 非 business_tasks 时应抛出配置异常。
    /// </summary>
    [Fact]
    public async Task ExecuteBatchAsync_ShouldThrow_WhenStatusDrivenTargetLogicalTableInvalid()
    {
        var service = CreateService();
        var context = CreateStatusDrivenContext(definition =>
        {
            definition.TargetLogicalTable = "sync_wms_status";
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteBatchAsync(context, CancellationToken.None));
        Assert.Contains("TargetLogicalTable 必须为 business_tasks", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 当 SourceType 为 Unknown 时应抛出配置异常。
    /// </summary>
    [Fact]
    public async Task ExecuteBatchAsync_ShouldThrow_WhenStatusDrivenSourceTypeUnknown()
    {
        var service = CreateService();
        var context = CreateStatusDrivenContext(definition =>
        {
            definition.SourceType = BusinessTaskSourceType.Unknown;
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteBatchAsync(context, CancellationToken.None));
        Assert.Contains("SourceType 不能为 Unknown", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 当 BusinessKeyColumn 为空白时应抛出配置异常。
    /// </summary>
    [Fact]
    public async Task ExecuteBatchAsync_ShouldThrow_WhenStatusDrivenBusinessKeyColumnBlank()
    {
        var service = CreateService();
        var context = CreateStatusDrivenContext(definition =>
        {
            definition.BusinessKeyColumn = "  ";
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteBatchAsync(context, CancellationToken.None));
        Assert.Contains("BusinessKeyColumn 不能为空白", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 创建待测服务。
    /// </summary>
    /// <returns>同步执行服务实例。</returns>
    private static SyncExecutionService CreateService()
    {
        return new SyncExecutionService(
            new NotInvokedOracleSourceReader(),
            new NotInvokedSyncStagingRepository(),
            new NotInvokedSyncUpsertRepository(),
            new NotInvokedDeletionExecutionService(),
            new NotInvokedSyncBatchRepository(),
            new NotInvokedSyncChangeLogRepository(),
            new NotInvokedSyncDeletionLogRepository(),
            new NotInvokedSyncCheckpointRepository(),
            new NotInvokedBusinessTaskStatusConsumeService(),
            NullLogger<SyncExecutionService>.Instance);
    }

    /// <summary>
    /// 创建未命中路径异常。
    /// </summary>
    /// <returns>异常实例。</returns>
    private static NotSupportedException CreateUnexpectedCallException()
    {
        return new NotSupportedException(UnexpectedCallMessage);
    }

    /// <summary>
    /// 创建状态驱动执行上下文。
    /// </summary>
    /// <param name="configure">定义定制器。</param>
    /// <returns>执行上下文。</returns>
    private static SyncExecutionContext CreateStatusDrivenContext(Action<SyncTableDefinition> configure)
    {
        var definition = new SyncTableDefinition
        {
            TableCode = "WmsSplitPickToLightCarton",
            SyncMode = SyncMode.StatusDriven,
            TargetLogicalTable = "business_tasks",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKeyColumn = "CARTONNO",
        };
        configure(definition);
        return new SyncExecutionContext
        {
            BatchId = "batch-001",
            Definition = definition,
            Checkpoint = new SyncCheckpoint
            {
                TableCode = definition.TableCode,
            },
            Window = new SyncWindow(DateTime.Now.AddMinutes(-5), DateTime.Now),
        };
    }

    /// <summary>
    /// 未命中路径的 Oracle 读取器替身。
    /// </summary>
    private sealed class NotInvokedOracleSourceReader : IOracleSourceReader
    {
        public Task<SyncReadResult> ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 未命中路径的暂存仓储替身。
    /// </summary>
    private sealed class NotInvokedSyncStagingRepository : ISyncStagingRepository
    {
        public Task BulkInsertAsync(string batchId, int pageNo, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, IReadOnlySet<string> normalizedExcludedColumns, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPageRowsAsync(string batchId, int pageNo, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task ClearPageAsync(string batchId, int pageNo, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 未命中路径的合并仓储替身。
    /// </summary>
    private sealed class NotInvokedSyncUpsertRepository : ISyncUpsertRepository
    {
        public Task<SyncMergeResult> MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsAsync(string tableCode, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 未命中路径的删除执行服务替身。
    /// </summary>
    private sealed class NotInvokedDeletionExecutionService : IDeletionExecutionService
    {
        public Task<SyncDeletionExecutionResult> ExecuteDeletionAsync(SyncExecutionContext context, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 未命中路径的批次仓储替身。
    /// </summary>
    private sealed class NotInvokedSyncBatchRepository : ISyncBatchRepository
    {
        public Task CreateBatchAsync(SyncBatch batch, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 未命中路径的变更日志仓储替身。
    /// </summary>
    private sealed class NotInvokedSyncChangeLogRepository : ISyncChangeLogRepository
    {
        public Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 未命中路径的删除日志仓储替身。
    /// </summary>
    private sealed class NotInvokedSyncDeletionLogRepository : ISyncDeletionLogRepository
    {
        public Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 未命中路径的检查点仓储替身。
    /// </summary>
    private sealed class NotInvokedSyncCheckpointRepository : ISyncCheckpointRepository
    {
        public Task<SyncCheckpoint> GetAsync(string tableCode, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 未命中路径的状态驱动消费服务替身。
    /// </summary>
    private sealed class NotInvokedBusinessTaskStatusConsumeService : IBusinessTaskStatusConsumeService
    {
        public Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, SyncWindow window, CancellationToken ct) => throw CreateUnexpectedCallException();
    }
}
