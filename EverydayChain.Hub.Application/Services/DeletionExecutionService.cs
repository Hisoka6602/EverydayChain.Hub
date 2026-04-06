using System.Text.Json;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 删除执行服务实现。
/// </summary>
public class DeletionExecutionService(
    ISyncDeletionRepository deletionRepository,
    ILogger<DeletionExecutionService> logger) : IDeletionExecutionService
{
    /// <summary>快照序列化配置。</summary>
    private static readonly JsonSerializerOptions SnapshotSerializerOptions = new()
    {
        WriteIndented = false,
    };

    /// <inheritdoc/>
    public async Task<SyncDeletionExecutionResult> ExecuteDeletionAsync(SyncExecutionContext context, CancellationToken ct)
    {
        if (!context.Definition.DeletionEnabled || context.Definition.DeletionPolicy == DeletionPolicy.Disabled)
        {
            return new SyncDeletionExecutionResult();
        }

        var candidates = await deletionRepository.DetectDeletedKeysAsync(new SyncDeletionDetectRequest
        {
            TableCode = context.Definition.TableCode,
            CursorColumn = context.Definition.CursorColumn,
            SourceSchema = context.Definition.SourceSchema,
            SourceTable = context.Definition.SourceTable,
            Window = context.Window,
            UniqueKeys = context.Definition.UniqueKeys,
            CompareSegmentSize = context.Definition.DeletionCompareSegmentSize,
            CompareMaxParallelism = context.Definition.DeletionCompareMaxParallelism,
        }, ct);

        var uniqueCandidates = candidates
            .GroupBy(x => x.BusinessKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var businessKeys = uniqueCandidates.Select(x => x.BusinessKey).ToList();
        var deletedCount = await deletionRepository.ApplyDeletionAsync(new SyncDeletionApplyRequest
        {
            TableCode = context.Definition.TableCode,
            BusinessKeys = businessKeys,
            DeletionPolicy = context.Definition.DeletionPolicy,
            DryRun = context.Definition.DeletionDryRun,
        }, ct);

        var deletionLogs = new List<SyncDeletionLog>(uniqueCandidates.Count);
        var changeLogs = new List<SyncChangeLog>(uniqueCandidates.Count);
        var executed = !context.Definition.DeletionDryRun && context.Definition.DeletionPolicy != DeletionPolicy.Disabled;
        // 仅在实际执行删除时才获取当前时间，dry-run 或禁用策略时跳过无意义的时间戳调用。
        var nowLocal = executed ? DateTime.Now : default;
        foreach (var candidate in uniqueCandidates)
        {
            ct.ThrowIfCancellationRequested();
            var snapshot = JsonSerializer.Serialize(candidate.TargetSnapshot, SnapshotSerializerOptions);
            deletionLogs.Add(new SyncDeletionLog
            {
                BatchId = context.BatchId,
                ParentBatchId = context.ParentBatchId,
                TableCode = context.Definition.TableCode,
                BusinessKey = candidate.BusinessKey,
                DeletionPolicy = context.Definition.DeletionPolicy,
                Executed = executed,
                DeletedTimeLocal = executed ? nowLocal : null,
                SourceEvidence = candidate.SourceEvidence,
            });
            if (executed)
            {
                changeLogs.Add(new SyncChangeLog
                {
                    BatchId = context.BatchId,
                    ParentBatchId = context.ParentBatchId,
                    TableCode = context.Definition.TableCode,
                    OperationType = SyncChangeOperationType.Delete,
                    BusinessKey = candidate.BusinessKey,
                    BeforeSnapshot = snapshot,
                    AfterSnapshot = null,
                    ChangedTimeLocal = nowLocal,
                });
            }
        }

        logger.LogInformation(
            "删除同步执行完成。TableCode={TableCode}, BatchId={BatchId}, DetectedCount={DetectedCount}, DeletedCount={DeletedCount}, DryRun={DryRun}, Policy={Policy}",
            context.Definition.TableCode,
            context.BatchId,
            uniqueCandidates.Count,
            deletedCount,
            context.Definition.DeletionDryRun,
            context.Definition.DeletionPolicy);

        return new SyncDeletionExecutionResult
        {
            DetectedCount = uniqueCandidates.Count,
            DeletedCount = deletedCount,
            DeletionLogs = deletionLogs,
            ChangeLogs = changeLogs,
        };
    }
}
