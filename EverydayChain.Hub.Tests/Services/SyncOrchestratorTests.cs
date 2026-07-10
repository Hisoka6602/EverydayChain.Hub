using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Runtime;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Services;

public sealed class SyncOrchestratorTests
{
    [Fact]
    public async Task RunTableSyncAsync_ShouldUseProcessScopedLeaseOwnerId_WhenTryingToAcquireLease()
    {
        var runtimeLeaseRepository = new CapturingRuntimeLeaseRepository
        {
            TryAcquireResult = false
        };
        var orchestrator = new SyncOrchestrator(
            new StubSyncTaskConfigRepository(),
            new StubSyncCheckpointRepository(),
            new StubSyncBatchRepository(),
            runtimeLeaseRepository,
            new StubSyncWindowCalculator(),
            new StubSyncExecutionService(),
            new StubDatabaseConnectivityService(),
            Options.Create(new SyncJobOptions()),
            NullLogger<SyncOrchestrator>.Instance);

        var result = await orchestrator.RunTableSyncAsync("WmsPickToWcs", CancellationToken.None);

        Assert.Equal("Table sync is already running.", result.FailureMessage);
        Assert.NotNull(runtimeLeaseRepository.CapturedOwnerId);
        Assert.True(RuntimeLeaseOwnerId.TryParse(runtimeLeaseRepository.CapturedOwnerId, out var descriptor));
        Assert.Equal(Environment.ProcessId, descriptor.ProcessId);
    }

    [Fact]
    public async Task RunTableSyncAsync_ShouldReturnFailureResult_WhenExecutionThrows()
    {
        var orchestrator = new SyncOrchestrator(
            new StubSyncTaskConfigRepository(),
            new StubSyncCheckpointRepository(),
            new StubSyncBatchRepository(),
            new CapturingRuntimeLeaseRepository
            {
                TryAcquireResult = true
            },
            new StubSyncWindowCalculator(),
            new ThrowingSyncExecutionService(new InvalidOperationException("Oracle is unavailable.")),
            new StubDatabaseConnectivityService(),
            Options.Create(new SyncJobOptions()),
            NullLogger<SyncOrchestrator>.Instance);

        var result = await orchestrator.RunTableSyncAsync("WmsPickToWcs", CancellationToken.None);

        Assert.Equal("WmsPickToWcs", result.TableCode);
        Assert.Equal(1.000M, result.FailureRate);
        Assert.Equal("Single-table sync failed: Oracle is unavailable.", result.FailureMessage);
    }

    [Fact]
    public async Task RunTableSyncAsync_ShouldSkipExecution_WhenDatabaseConnectivityIsUnavailable()
    {
        var runtimeLeaseRepository = new CapturingRuntimeLeaseRepository
        {
            TryAcquireResult = true
        };
        var executionService = new CountingSyncExecutionService();
        var orchestrator = new SyncOrchestrator(
            new StubSyncTaskConfigRepository(),
            new StubSyncCheckpointRepository(),
            new StubSyncBatchRepository(),
            runtimeLeaseRepository,
            new StubSyncWindowCalculator(),
            executionService,
            new StubDatabaseConnectivityService
            {
                Snapshot = CreateOracleUnavailableSnapshot()
            },
            Options.Create(new SyncJobOptions()),
            NullLogger<SyncOrchestrator>.Instance);

        var result = await orchestrator.RunTableSyncAsync("WmsPickToWcs", CancellationToken.None);

        Assert.Equal(0, executionService.CallCount);
        Assert.Null(runtimeLeaseRepository.CapturedOwnerId);
        Assert.Equal("WmsPickToWcs", result.TableCode);
        Assert.Equal(1.000M, result.FailureRate);
        Assert.Contains("Single-table sync failed:", result.FailureMessage, StringComparison.Ordinal);
        Assert.Contains("Oracle", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAllEnabledTableSyncAsync_ShouldReturnFailureResults_WhenDatabaseConnectivityIsUnavailable()
    {
        var executionService = new CountingSyncExecutionService();
        var orchestrator = new SyncOrchestrator(
            new StubSyncTaskConfigRepository(),
            new StubSyncCheckpointRepository(),
            new StubSyncBatchRepository(),
            new CapturingRuntimeLeaseRepository(),
            new StubSyncWindowCalculator(),
            executionService,
            new StubDatabaseConnectivityService
            {
                Snapshot = CreateOracleUnavailableSnapshot()
            },
            Options.Create(new SyncJobOptions()),
            NullLogger<SyncOrchestrator>.Instance);

        var results = await orchestrator.RunAllEnabledTableSyncAsync(CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(0, executionService.CallCount);
        Assert.All(results, result =>
        {
            Assert.Equal(1.000M, result.FailureRate);
            Assert.Contains("Oracle", result.FailureMessage ?? string.Empty, StringComparison.Ordinal);
        });
    }

    private sealed class StubSyncTaskConfigRepository : ISyncTaskConfigRepository
    {
        public Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct)
        {
            return Task.FromResult(new SyncTableDefinition
            {
                TableCode = tableCode,
                SourceSchema = "WMS",
                SourceTable = tableCode,
                CursorColumn = "UpdateTimeLocal",
                PageSize = 100
            });
        }

        public Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<SyncTableDefinition>>(
            [
                new SyncTableDefinition
                {
                    TableCode = "WmsPickToWcs",
                    SourceSchema = "WMS",
                    SourceTable = "IDX_PICKTOWCS2",
                    CursorColumn = "UpdateTimeLocal",
                    PageSize = 100
                },
                new SyncTableDefinition
                {
                    TableCode = "WmsSplitPickToLightCarton",
                    SourceSchema = "WMS",
                    SourceTable = "IDX_PICKTOLIGHT_CARTON1",
                    CursorColumn = "UpdateTimeLocal",
                    PageSize = 100
                }
            ]);
        }

        public Task<int> GetMaxParallelTablesAsync(CancellationToken ct)
        {
            return Task.FromResult(2);
        }
    }

    private sealed class StubSyncCheckpointRepository : ISyncCheckpointRepository
    {
        public Task<SyncCheckpoint> GetAsync(string tableCode, CancellationToken ct)
        {
            return Task.FromResult(new SyncCheckpoint
            {
                TableCode = tableCode
            });
        }

        public Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken ct)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubSyncBatchRepository : ISyncBatchRepository
    {
        public Task CreateBatchAsync(SyncBatch batch, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct)
        {
            return Task.FromResult<string?>(null);
        }

        public Task<IReadOnlyList<SyncBatch>> ListLatestByTableCodesAsync(IReadOnlyCollection<string> tableCodes, CancellationToken ct)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingRuntimeLeaseRepository : IRuntimeLeaseRepository
    {
        /// <summary>
        /// 获取或设置模拟抢占租约的返回结果。
        /// </summary>
        public bool TryAcquireResult { get; set; }

        /// <summary>
        /// 获取最近一次捕获到的租约持有者标识。
        /// </summary>
        public string? CapturedOwnerId { get; private set; }

        public Task<bool> TryAcquireAsync(string leaseKey, string ownerId, DateTime acquiredTimeLocal, DateTime expiresAtLocal, CancellationToken ct)
        {
            CapturedOwnerId = ownerId;
            return Task.FromResult(TryAcquireResult);
        }

        public Task<RuntimeLeaseSnapshot?> GetAsync(string leaseKey, CancellationToken ct)
        {
            return Task.FromResult<RuntimeLeaseSnapshot?>(null);
        }

        public Task ReleaseAsync(string leaseKey, string ownerId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubSyncWindowCalculator : ISyncWindowCalculator
    {
        public SyncWindow CalculateWindow(SyncTableDefinition definition, SyncCheckpoint checkpoint, DateTime nowLocal)
        {
            return new SyncWindow(nowLocal.AddMinutes(-1), nowLocal);
        }
    }

    private sealed class StubSyncExecutionService : ISyncExecutionService
    {
        public Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CountingSyncExecutionService : ISyncExecutionService
    {
        /// <summary>
        /// 获取执行服务被调用的次数。
        /// </summary>
        public int CallCount { get; private set; }

        public Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new SyncBatchResult
            {
                BatchId = Guid.NewGuid().ToString("N"),
                TableCode = context.Definition.TableCode
            });
        }
    }

    private sealed class ThrowingSyncExecutionService(Exception exception) : ISyncExecutionService
    {
        public Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct)
        {
            throw exception;
        }
    }

    private sealed class StubDatabaseConnectivityService : IDatabaseConnectivityService
    {
        /// <summary>
        /// 获取或设置当前返回的数据库连通性快照。
        /// </summary>
        public DatabaseConnectivitySnapshot Snapshot { get; set; } = new()
        {
            LocalSqlServer = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "LocalSqlServer",
                IsAvailable = true,
                Description = "LocalSqlServer available"
            },
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "Oracle",
                IsAvailable = true,
                Description = "Oracle available"
            }
        };

        public Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot);
        }

        public Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot);
        }

        public Task<DatabaseEndpointConnectivityState> GetLocalSqlServerStateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot.LocalSqlServer);
        }

        public Task<DatabaseEndpointConnectivityState> RefreshLocalSqlServerStateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot.LocalSqlServer);
        }

        public bool IsDatabaseConnectivityException(Exception exception)
        {
            return false;
        }
    }

    private static DatabaseConnectivitySnapshot CreateOracleUnavailableSnapshot()
    {
        return new DatabaseConnectivitySnapshot
        {
            LocalSqlServer = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "LocalSqlServer",
                IsAvailable = true,
                Description = "LocalSqlServer available"
            },
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "Oracle",
                IsAvailable = false,
                Description = "Oracle unavailable"
            }
        };
    }
}
