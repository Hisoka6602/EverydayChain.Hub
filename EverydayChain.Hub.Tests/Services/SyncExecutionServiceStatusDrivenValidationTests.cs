using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 SyncExecutionServiceStatusDrivenValidationTests 类型。
/// </summary>
public class SyncExecutionServiceStatusDrivenValidationTests
{
    /// <summary>
    /// 存储 UnexpectedCallMessage 字段。
    /// </summary>
    private const string UnexpectedCallMessage = "此方法不应在配置校验测试路径中被调用。";

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

    [Fact]
    public async Task ExecuteBatchAsync_ShouldAdvanceCheckpoint_WhenStatusDrivenConsumeReturnsCursor()
    {
        var expectedCursor = new DateTime(2032, 7, 1, 12, 37, 48, DateTimeKind.Local);
        var checkpointRepository = new CapturingSyncCheckpointRepository();
        var service = new SyncExecutionService(
            new NotInvokedOracleSourceReader(),
            new NotInvokedSyncStagingRepository(),
            new NotInvokedSyncUpsertRepository(),
            new NotInvokedDeletionExecutionService(),
            new CapturingSyncBatchRepository(),
            new NoopSyncChangeLogRepository(),
            new NoopSyncDeletionLogRepository(),
            checkpointRepository,
            new ReturningBusinessTaskStatusConsumeService(new RemoteStatusConsumeResult
            {
                ReadCount = 2,
                AppendCount = 2,
                PageCount = 1,
                LastSuccessCursorLocal = expectedCursor,
            }),
            NullLogger<SyncExecutionService>.Instance);
        var context = CreateStatusDrivenContext(definition =>
        {
            definition.StatusConsumeProfile = new RemoteStatusConsumeProfile
            {
                StatusColumnName = "TASKPROCESS",
                PendingStatusValue = "N",
                CompletedStatusValue = "Y",
                ShouldWriteBackRemoteStatus = false,
                BatchSize = 500,
            };
        });

        var result = await service.ExecuteBatchAsync(context, CancellationToken.None);

        Assert.Equal(2, result.ReadCount);
        Assert.Equal(2, result.InsertCount);
        Assert.NotNull(checkpointRepository.SavedCheckpoint);
        Assert.Equal(expectedCursor, checkpointRepository.SavedCheckpoint!.LastSuccessCursorLocal);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ShouldKeepCheckpoint_WhenStatusDrivenWriteBackFails()
    {
        var oldCursor = new DateTime(2032, 7, 1, 8, 0, 0, DateTimeKind.Local);
        var candidateCursor = new DateTime(2032, 7, 1, 12, 37, 48, DateTimeKind.Local);
        var checkpointRepository = new CapturingSyncCheckpointRepository();
        var service = new SyncExecutionService(
            new NotInvokedOracleSourceReader(),
            new NotInvokedSyncStagingRepository(),
            new NotInvokedSyncUpsertRepository(),
            new NotInvokedDeletionExecutionService(),
            new CapturingSyncBatchRepository(),
            new NoopSyncChangeLogRepository(),
            new NoopSyncDeletionLogRepository(),
            checkpointRepository,
            new ReturningBusinessTaskStatusConsumeService(new RemoteStatusConsumeResult
            {
                ReadCount = 2,
                AppendCount = 2,
                PageCount = 1,
                WriteBackFailCount = 1,
                LastSuccessCursorLocal = candidateCursor,
            }),
            NullLogger<SyncExecutionService>.Instance);
        var context = CreateStatusDrivenContext(definition =>
        {
            definition.StatusConsumeProfile = new RemoteStatusConsumeProfile
            {
                StatusColumnName = "TASKPROCESS",
                PendingStatusValue = "N",
                CompletedStatusValue = "Y",
                ShouldWriteBackRemoteStatus = true,
                BatchSize = 500,
            };
        });
        context.Checkpoint.LastSuccessCursorLocal = oldCursor;

        await service.ExecuteBatchAsync(context, CancellationToken.None);

        Assert.NotNull(checkpointRepository.SavedCheckpoint);
        Assert.Equal(oldCursor, checkpointRepository.SavedCheckpoint!.LastSuccessCursorLocal);
    }

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

    private static NotSupportedException CreateUnexpectedCallException()
    {
        return new NotSupportedException(UnexpectedCallMessage);
    }

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
    /// 定义 NotInvokedOracleSourceReader 类型。
    /// </summary>
    private sealed class NotInvokedOracleSourceReader : IOracleSourceReader
    {
        public Task<SyncReadResult> ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadRowsByBusinessKeysAsync(OracleBusinessKeyRowReadRequest request, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 定义 NotInvokedSyncStagingRepository 类型。
    /// </summary>
    private sealed class NotInvokedSyncStagingRepository : ISyncStagingRepository
    {
        public Task BulkInsertAsync(string batchId, int pageNo, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, IReadOnlySet<string> normalizedExcludedColumns, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPageRowsAsync(string batchId, int pageNo, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task ClearPageAsync(string batchId, int pageNo, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 定义 NotInvokedSyncUpsertRepository 类型。
    /// </summary>
    private sealed class NotInvokedSyncUpsertRepository : ISyncUpsertRepository
    {
        public Task<SyncMergeResult> MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsAsync(string tableCode, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 定义 NotInvokedDeletionExecutionService 类型。
    /// </summary>
    private sealed class NotInvokedDeletionExecutionService : IDeletionExecutionService
    {
        public Task<SyncDeletionExecutionResult> ExecuteDeletionAsync(SyncExecutionContext context, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    /// <summary>
    /// 定义 NotInvokedSyncBatchRepository 类型。
    /// </summary>
    private sealed class NotInvokedSyncBatchRepository : ISyncBatchRepository
    {
        public Task CreateBatchAsync(SyncBatch batch, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task<IReadOnlyList<SyncBatch>> ListLatestByTableCodesAsync(IReadOnlyCollection<string> tableCodes, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    private sealed class CapturingSyncBatchRepository : ISyncBatchRepository
    {
        /// <summary>
        /// 执行 CreateBatchAsync 方法。
        /// </summary>
        public Task CreateBatchAsync(SyncBatch batch, CancellationToken ct) => Task.CompletedTask;

        /// <summary>
        /// 执行 MarkInProgressAsync 方法。
        /// </summary>
        public Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct) => Task.CompletedTask;

        /// <summary>
        /// 执行 CompleteBatchAsync 方法。
        /// </summary>
        public Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct) => Task.CompletedTask;

        /// <summary>
        /// 执行 FailBatchAsync 方法。
        /// </summary>
        public Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct) => Task.CompletedTask;

        /// <summary>
        /// 执行 GetLatestFailedBatchIdAsync 方法。
        /// </summary>
        public Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct) => Task.FromResult<string?>(null);

        /// <summary>
        /// 执行 ListLatestByTableCodesAsync 方法。
        /// </summary>
        public Task<IReadOnlyList<SyncBatch>> ListLatestByTableCodesAsync(IReadOnlyCollection<string> tableCodes, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<SyncBatch>>([]);
        }
    }

    /// <summary>
    /// 定义 NotInvokedSyncChangeLogRepository 类型。
    /// </summary>
    private sealed class NotInvokedSyncChangeLogRepository : ISyncChangeLogRepository
    {
        public Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    private sealed class NoopSyncChangeLogRepository : ISyncChangeLogRepository
    {
        /// <summary>
        /// 执行 WriteChangesAsync 方法。
        /// </summary>
        public Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>
    /// 定义 NotInvokedSyncDeletionLogRepository 类型。
    /// </summary>
    private sealed class NotInvokedSyncDeletionLogRepository : ISyncDeletionLogRepository
    {
        public Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    private sealed class NoopSyncDeletionLogRepository : ISyncDeletionLogRepository
    {
        /// <summary>
        /// 执行 WriteDeletionsAsync 方法。
        /// </summary>
        public Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>
    /// 定义 NotInvokedSyncCheckpointRepository 类型。
    /// </summary>
    private sealed class NotInvokedSyncCheckpointRepository : ISyncCheckpointRepository
    {
        public Task<SyncCheckpoint> GetAsync(string tableCode, CancellationToken ct) => throw CreateUnexpectedCallException();
        public Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    private sealed class CapturingSyncCheckpointRepository : ISyncCheckpointRepository
    {
        /// <summary>
        /// 获取或设置 SavedCheckpoint。
        /// </summary>
        public SyncCheckpoint? SavedCheckpoint { get; private set; }

        /// <summary>
        /// 执行 GetAsync 方法。
        /// </summary>
        public Task<SyncCheckpoint> GetAsync(string tableCode, CancellationToken ct)
        {
            return Task.FromResult(new SyncCheckpoint { TableCode = tableCode });
        }

        /// <summary>
        /// 执行 SaveAsync 方法。
        /// </summary>
        public Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken ct)
        {
            SavedCheckpoint = checkpoint;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 定义 NotInvokedBusinessTaskStatusConsumeService 类型。
    /// </summary>
    private sealed class NotInvokedBusinessTaskStatusConsumeService : IBusinessTaskStatusConsumeService
    {
        public Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, SyncWindow window, CancellationToken ct) => throw CreateUnexpectedCallException();
    }

    private sealed class ReturningBusinessTaskStatusConsumeService(RemoteStatusConsumeResult result) : IBusinessTaskStatusConsumeService
    {
        /// <summary>
        /// 执行 ConsumeAsync 方法。
        /// </summary>
        public Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, SyncWindow window, CancellationToken ct)
        {
            return Task.FromResult(result);
        }
    }
}

