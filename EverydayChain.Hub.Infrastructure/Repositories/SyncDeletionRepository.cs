using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步删除仓储基础实现（基于内存目标数据对比）。
/// </summary>
public class SyncDeletionRepository(IOracleSourceReader oracleSourceReader, ISyncUpsertRepository upsertRepository) : ISyncDeletionRepository
{
    /// <summary>源端缺失证据描述。</summary>
    private const string MissingSourceEvidenceMessage = "窗口内源端未检索到该业务键。";
    /// <summary>删除差异比对默认分段大小。</summary>
    private const int DefaultCompareSegmentSize = 20000;
    /// <summary>删除差异比对默认并行度。</summary>
    private const int DefaultCompareMaxParallelism = 1;
    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncDeletionCandidate>> DetectDeletedKeysAsync(SyncDeletionDetectRequest request, CancellationToken ct)
    {
        var sourceKeys = await oracleSourceReader.ReadByKeysAsync(new SyncKeyReadRequest
        {
            TableCode = request.TableCode,
            SourceSchema = request.SourceSchema,
            SourceTable = request.SourceTable,
            CursorColumn = request.CursorColumn,
            Window = request.Window,
            UniqueKeys = request.UniqueKeys,
        }, ct);

        var targetRows = await upsertRepository.ListTargetStateRowsAsync(request.TableCode, ct);
        var candidates = new List<SyncDeletionCandidate>();
        var segmentSize = request.CompareSegmentSize > 0 ? request.CompareSegmentSize : DefaultCompareSegmentSize;
        var maxParallelism = request.CompareMaxParallelism > 0 ? request.CompareMaxParallelism : DefaultCompareMaxParallelism;

        if (maxParallelism <= 1)
        {
            // 并行度为 1 时使用顺序循环，避免 ConcurrentBag / Parallel.ForAsync 的额外开销。
            for (var rowIndex = 0; rowIndex < targetRows.Count; rowIndex++)
            {
                ct.ThrowIfCancellationRequested();
                var row = targetRows[rowIndex];

                if (!IsRowWithinWindow(row, request.Window))
                {
                    continue;
                }

                var businessKey = row.BusinessKey;
                if (string.IsNullOrWhiteSpace(businessKey) || sourceKeys.Contains(businessKey))
                {
                    continue;
                }

                candidates.Add(new SyncDeletionCandidate
                {
                    BusinessKey = businessKey,
                    TargetSnapshot = new Dictionary<string, object?>
                    {
                        [nameof(SyncTargetStateRow.BusinessKey)] = row.BusinessKey,
                        [nameof(SyncTargetStateRow.RowDigest)] = row.RowDigest,
                        [nameof(SyncTargetStateRow.CursorLocal)] = row.CursorLocal,
                        [nameof(SyncTargetStateRow.IsSoftDeleted)] = row.IsSoftDeleted,
                        [nameof(SyncTargetStateRow.SoftDeletedTimeLocal)] = row.SoftDeletedTimeLocal,
                    },
                    SourceEvidence = MissingSourceEvidenceMessage,
                });
            }
        }
        else
        {
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = maxParallelism,
            };

            var candidateBag = new System.Collections.Concurrent.ConcurrentBag<SyncDeletionCandidate>();
            var segmentCount = (targetRows.Count + segmentSize - 1) / segmentSize;
            await Parallel.ForAsync(0, segmentCount, parallelOptions, (segmentIndex, token) =>
            {
                var startIndex = segmentIndex * segmentSize;
                var endExclusive = Math.Min(startIndex + segmentSize, targetRows.Count);
                for (var rowIndex = startIndex; rowIndex < endExclusive; rowIndex++)
                {
                    token.ThrowIfCancellationRequested();
                    var row = targetRows[rowIndex];

                    if (!IsRowWithinWindow(row, request.Window))
                    {
                        continue;
                    }

                    var businessKey = row.BusinessKey;
                    if (string.IsNullOrWhiteSpace(businessKey) || sourceKeys.Contains(businessKey))
                    {
                        continue;
                    }

                    candidateBag.Add(new SyncDeletionCandidate
                    {
                        BusinessKey = businessKey,
                        TargetSnapshot = new Dictionary<string, object?>
                        {
                            [nameof(SyncTargetStateRow.BusinessKey)] = row.BusinessKey,
                            [nameof(SyncTargetStateRow.RowDigest)] = row.RowDigest,
                            [nameof(SyncTargetStateRow.CursorLocal)] = row.CursorLocal,
                            [nameof(SyncTargetStateRow.IsSoftDeleted)] = row.IsSoftDeleted,
                            [nameof(SyncTargetStateRow.SoftDeletedTimeLocal)] = row.SoftDeletedTimeLocal,
                        },
                        SourceEvidence = MissingSourceEvidenceMessage,
                    });
                }

                return ValueTask.CompletedTask;
            });

            candidates.AddRange(candidateBag);
        }

        // 统一按 BusinessKey 排序，保证无论并行度配置如何，输出顺序均一致稳定。
        candidates.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.BusinessKey, b.BusinessKey));

        return candidates;
    }

    /// <summary>
    /// 判断目标数据行是否在同步窗口内。
    /// </summary>
    /// <param name="row">目标状态行。</param>
    /// <param name="window">同步窗口。</param>
    /// <returns>在窗口内返回 <c>true</c>。</returns>
    private static bool IsRowWithinWindow(SyncTargetStateRow row, SyncWindow window)
    {
        if (!row.CursorLocal.HasValue)
        {
            return false;
        }

        var cursorLocal = row.CursorLocal.Value;
        if (cursorLocal.Kind == DateTimeKind.Unspecified)
        {
            cursorLocal = DateTime.SpecifyKind(cursorLocal, DateTimeKind.Local);
        }

        if (cursorLocal.Kind != DateTimeKind.Local)
        {
            return false;
        }

        return cursorLocal > window.WindowStartLocal && cursorLocal <= window.WindowEndLocal;
    }

    /// <inheritdoc/>
    public Task<int> ApplyDeletionAsync(SyncDeletionApplyRequest request, CancellationToken ct)
    {
        if (request.BusinessKeys.Count == 0)
        {
            return Task.FromResult(0);
        }

        if (request.DeletionPolicy == DeletionPolicy.Disabled)
        {
            return Task.FromResult(0);
        }

        if (request.DryRun)
        {
            return Task.FromResult(0);
        }

        return upsertRepository.DeleteByBusinessKeysAsync(request.TableCode, request.BusinessKeys, request.DeletionPolicy, ct);
    }
}
